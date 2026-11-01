using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Dward.Analyzers.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NSubstituteSequentialReturnsCodeFixProvider)), Shared]
public class NSubstituteSequentialReturnsCodeFixProvider : CodeFixProvider
{
    private const string Title = "Use a typed empty sequential return value";
    private static readonly FixAllProvider _fixAllProvider = FixAllProvider.Create(FixDocument);
    private static readonly SymbolDisplayFormat _typeDisplayFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .WithMiscellaneousOptions(SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public override ImmutableArray<string> FixableDiagnosticIds => ["DWARD0003"];

    public override FixAllProvider GetFixAllProvider() => _fixAllProvider;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var repairedDocument = await CreateRepair(
                context.Document, diagnostic.Location.SourceSpan, context.CancellationToken).ConfigureAwait(false);
            if (repairedDocument is null)
            {
                continue;
            }

            var action = CodeAction.Create(Title, _ => Task.FromResult(repairedDocument), equivalenceKey: Title);
            context.RegisterCodeFix(action, diagnostic);
        }
    }

    private static async Task<Document?> CreateRepair(Document document, TextSpan span, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (root?.FindNode(span, getInnermostNodeForTie: true) is not CollectionExpressionSyntax { Elements.Count: 0 } empty
            || empty.Parent is not ArgumentSyntax argument
            || argument.Parent?.Parent is not InvocationExpressionSyntax invocation
            || semanticModel?.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation
            || operation.TargetMethod is not { Name: "Returns", TypeArguments.Length: 1 } method
            || method.ContainingType.ToDisplayString() != "NSubstitute.SubstituteExtensions"
            || method.ContainingAssembly.Identity.Name != "NSubstitute"
            || !operation.Arguments.Any(candidate =>
                candidate.Syntax == argument && candidate.Parameter?.Ordinal == 2 && candidate.ArgumentKind == ArgumentKind.Explicit))
        {
            return null;
        }

        var ancestors = empty.Ancestors().TakeWhile(node => node is not StatementSyntax).ToArray();
        if (ancestors.Any(node => node is AnonymousFunctionExpressionSyntax or ArrowExpressionClauseSyntax)
            || empty.FirstAncestorOrSelf<StatementSyntax>() is not { Parent: BlockSyntax block } statement)
        {
            return null;
        }

        var usedNames = new HashSet<string>(
            root.DescendantTokens().Where(token => token.IsKind(SyntaxKind.IdentifierToken)).Select(token => token.ValueText),
            StringComparer.Ordinal);
        var name = "emptyReturnValue";
        for (var suffix = 2; usedNames.Contains(name) || semanticModel.LookupSymbols(statement.SpanStart, name: name).Length != 0; suffix++)
        {
            name = $"emptyReturnValue{suffix}";
        }

        var returnType = method.TypeArguments[0];
        var typeName = returnType.ToDisplayString(_typeDisplayFormat);
        var typeSyntax = SyntaxFactory.ParseTypeName(typeName).WithAdditionalAnnotations(Simplifier.Annotation);
        var declaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(typeSyntax)
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(name))))
            .WithLeadingTrivia(statement.GetLeadingTrivia())
            .WithAdditionalAnnotations(Formatter.Annotation);

        // Construct the empty value at the original argument position, not before receiver/argument side effects.
        var assignment = SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName(name), empty.WithoutTrivia())
            .WithTriviaFrom(empty)
            .WithAdditionalAnnotations(Formatter.Annotation);
        var invocationAnnotation = new SyntaxAnnotation();
        var replacementInvocation = invocation.ReplaceNode(empty, assignment).WithAdditionalAnnotations(invocationAnnotation);
        var replacementStatement = statement.ReplaceNode(invocation, replacementInvocation)
            .WithLeadingTrivia(SyntaxFactory.ElasticMarker)
            .WithAdditionalAnnotations(Formatter.Annotation);
        var statementIndex = block.Statements.IndexOf(statement);
        var statements = block.Statements.Replace(statement, replacementStatement).Insert(statementIndex, declaration);
        var replacementRoot = root.ReplaceNode(block, block.WithStatements(statements));
        var repairedDocument = document.WithSyntaxRoot(replacementRoot);
        var repairedModel = await repairedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var repairedRoot = await repairedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var repairedInvocation = repairedRoot?.GetAnnotatedNodes(invocationAnnotation).Single();
        if (repairedModel is null
            || repairedInvocation is null
            || repairedModel.GetDiagnostics(cancellationToken: cancellationToken).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            || repairedModel.GetOperation(repairedInvocation, cancellationToken) is not IInvocationOperation repairedOperation
            || repairedOperation.TargetMethod.ToDisplayString(_typeDisplayFormat)
                != method.ToDisplayString(_typeDisplayFormat)
            || !repairedOperation.Arguments.Any(candidate =>
                candidate.Parameter?.Ordinal == 2
                && candidate.ArgumentKind == ArgumentKind.ParamArray
                && candidate.Value is IArrayCreationOperation { Initializer.ElementValues.Length: 1 }))
        {
            return null;
        }

        return repairedDocument;
    }

    private static async Task<Document?> FixDocument(
        FixAllContext context,
        Document document,
        ImmutableArray<Diagnostic> diagnostics)
    {
        var cancellationToken = context.CancellationToken;
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return null;
        }

        var targets = diagnostics.Select(diagnostic => root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true))
            .OfType<CollectionExpressionSyntax>().Distinct().OrderBy(target => target.SpanStart).ToArray();
        var annotations = targets.ToDictionary(target => target, _ => new SyntaxAnnotation());
        root = root.ReplaceNodes(targets, (original, rewritten) => rewritten.WithAdditionalAnnotations(annotations[original]));
        document = document.WithSyntaxRoot(root);

        // Recompute names and binding after each edit; independent batch edits could introduce identical locals.
        foreach (var originalTarget in targets)
        {
            root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Cannot repair sequential returns without a syntax root.");
            var target = root.GetAnnotatedNodes(annotations[originalTarget]).Single();
            var repairedDocument = await CreateRepair(document, target.Span, cancellationToken).ConfigureAwait(false);
            if (repairedDocument is not null)
            {
                document = repairedDocument;
            }
        }

        return document;
    }
}
