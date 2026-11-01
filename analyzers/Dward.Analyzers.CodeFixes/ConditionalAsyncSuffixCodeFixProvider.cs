using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace Dward.Analyzers.CodeFixes;

static class ConservativeAsyncRename
{
    public static async Task<Solution?> Create(Document document, TextSpan diagnosticSpan, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var token = root?.FindToken(diagnosticSpan.Start);
        var declaration = token?.Parent;
        if (token?.Span != diagnosticSpan
            || declaration is not (MethodDeclarationSyntax or LocalFunctionStatementSyntax)
            || model?.GetDeclaredSymbol(declaration, cancellationToken) is not IMethodSymbol method
            || !method.IsAsync
            || !method.Name.EndsWith("Async", StringComparison.Ordinal))
        {
            return null;
        }

        var newName = method.Name.Substring(0, method.Name.Length - "Async".Length);
        if (!SyntaxFacts.IsValidIdentifier(newName)
            || newName.EndsWith("Async", StringComparison.Ordinal)
            || !IsEligible(method, declaration, model, newName))
        {
            return null;
        }

        var solution = document.Project.Solution;
        var allowedEdits = new Dictionary<DocumentId, HashSet<TextSpan>>();
        if (!await AddEditableToken(document, diagnosticSpan, method.Name, newName, allowedEdits, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var references = await SymbolFinder.FindReferencesAsync(method, solution, cancellationToken).ConfigureAwait(false);
        foreach (var referencedSymbol in references)
        {
            if (!SymbolEqualityComparer.Default.Equals(referencedSymbol.Definition.OriginalDefinition, method.OriginalDefinition))
            {
                return null;
            }

            foreach (var reference in referencedSymbol.Locations)
            {
                if (reference.IsCandidateLocation)
                {
                    var referenceRoot = await reference.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var referenceModel = await reference.Document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var referenceToken = referenceRoot?.FindToken(reference.Location.SourceSpan.Start, findInsideTrivia: true);
                    var name = referenceToken?.Parent?.FirstAncestorOrSelf<SimpleNameSyntax>();
                    var nameofExpression = name?.Ancestors().OfType<InvocationExpressionSyntax>()
                        .FirstOrDefault(invocation => invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" });
                    if (name is null || nameofExpression is null || referenceModel is null)
                    {
                        return null;
                    }
                    var symbolInfo = referenceModel.GetSymbolInfo(name, cancellationToken);
                    if (symbolInfo.CandidateSymbols.Length != 1
                        || !SymbolEqualityComparer.Default.Equals(symbolInfo.CandidateSymbols[0].OriginalDefinition, method.OriginalDefinition))
                    {
                        return null;
                    }
                }

                if (reference.IsImplicit
                    || solution.GetDocument(reference.Document.Id) is null
                    || !await AddEditableToken(
                        reference.Document, reference.Location.SourceSpan, method.Name, newName, allowedEdits, cancellationToken).ConfigureAwait(false))
                {
                    return null;
                }
            }
        }

        if (await HasUntrackedNameReference(solution, method.Name, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var preparedCrefs = await PrepareCrefs(solution, method, newName, allowedEdits, cancellationToken).ConfigureAwait(false);
        if (preparedCrefs is null)
        {
            return null;
        }

        var renameDocument = preparedCrefs.Value.Solution.GetDocument(document.Id)
            ?? throw new InvalidOperationException("The prepared rename document is missing.");
        var renameRoot = await renameDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var renameModel = await renameDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var renameDeclaration = renameRoot?.FindToken(diagnosticSpan.Start).Parent;
        if (renameDeclaration is null || renameModel?.GetDeclaredSymbol(renameDeclaration, cancellationToken) is not IMethodSymbol renameMethod)
        {
            return null;
        }

        var options = new SymbolRenameOptions(
            RenameOverloads: false, RenameInStrings: false, RenameInComments: false, RenameFile: false);
        var renamedSolution = await Renamer.RenameSymbolAsync(
            preparedCrefs.Value.Solution, renameMethod, options, newName, cancellationToken).ConfigureAwait(false);

        // Keep bound cref references current without enabling textual comment renaming.
        foreach (var pair in preparedCrefs.Value.Annotations)
        {
            var renamedDocument = renamedSolution.GetDocument(pair.Key)
                ?? throw new InvalidOperationException("The documentation rename document is missing.");
            var renamedRoot = await renamedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The documentation rename syntax root is missing.");
            foreach (var annotation in pair.Value)
            {
                if (renamedRoot.GetAnnotatedNodes(annotation).SingleOrDefault() is not NameMemberCrefSyntax { Name: SimpleNameSyntax name } cref)
                {
                    return null;
                }
                var identifier = SyntaxFactory.Identifier(newName).WithTriviaFrom(name.Identifier);
                renamedRoot = renamedRoot.ReplaceNode(cref, cref.WithName(name.WithIdentifier(identifier)));
            }
            renamedSolution = renamedSolution.WithDocumentSyntaxRoot(pair.Key, renamedRoot);
        }

        var changes = renamedSolution.GetChanges(solution);
        if (changes.GetAddedProjects().Any() || changes.GetRemovedProjects().Any())
        {
            return null;
        }

        var changedProjects = changes.GetProjectChanges().ToArray();
        if (changedProjects.Length == 0)
        {
            return null;
        }

        foreach (var projectChanges in changedProjects)
        {
            if (projectChanges.GetAddedDocuments().Any() || projectChanges.GetRemovedDocuments().Any()
                || projectChanges.GetChangedAdditionalDocuments().Any() || projectChanges.GetChangedAnalyzerConfigDocuments().Any())
            {
                return null;
            }

            foreach (var documentId in projectChanges.GetChangedDocuments())
            {
                if (!allowedEdits.TryGetValue(documentId, out var spans))
                {
                    return null;
                }

                var originalDocument = solution.GetDocument(documentId)
                    ?? throw new InvalidOperationException("The original rename document is missing.");
                var renamedDocument = renamedSolution.GetDocument(documentId)
                    ?? throw new InvalidOperationException("The renamed document is missing.");
                var originalText = await originalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var renamedText = await renamedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var expectedText = originalText.WithChanges(spans.Select(span => new TextChange(span, newName)));
                if (!expectedText.ContentEquals(renamedText))
                {
                    return null;
                }
            }

            var originalProject = solution.GetProject(projectChanges.ProjectId)
                ?? throw new InvalidOperationException("The original rename project is missing.");
            var renamedProject = renamedSolution.GetProject(projectChanges.ProjectId)
                ?? throw new InvalidOperationException("The renamed project is missing.");
            var originalCompilation = await originalProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var renamedCompilation = await renamedProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (originalCompilation is null || renamedCompilation is null
                || originalCompilation.GetDiagnostics(cancellationToken).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                || renamedCompilation.GetDiagnostics(cancellationToken).Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            {
                return null;
            }
        }

        return renamedSolution;
    }

    private static async Task<(Solution Solution, Dictionary<DocumentId, List<SyntaxAnnotation>> Annotations)?> PrepareCrefs(
        Solution solution,
        IMethodSymbol method,
        string newName,
        Dictionary<DocumentId, HashSet<TextSpan>> allowedEdits,
        CancellationToken cancellationToken)
    {
        var annotatedSolution = solution;
        var annotations = new Dictionary<DocumentId, List<SyntaxAnnotation>>();
        foreach (var project in solution.Projects.Where(project => project.Language == LanguageNames.CSharp))
        {
            var generatedDocuments = await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);
            foreach (var document in project.Documents.Concat(generatedDocuments))
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (root is null)
                {
                    return null;
                }
                var crefs = root.DescendantNodes(descendIntoTrivia: true).OfType<NameMemberCrefSyntax>()
                    .Where(cref => cref.Name is SimpleNameSyntax name && name.Identifier.ValueText == method.Name).ToArray();
                if (crefs.Length == 0)
                {
                    continue;
                }
                var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                if (model is null)
                {
                    return null;
                }
                var targets = new Dictionary<NameMemberCrefSyntax, SyntaxAnnotation>();
                foreach (var cref in crefs)
                {
                    var symbolInfo = model.GetSymbolInfo(cref, cancellationToken);
                    var referencedSymbol = symbolInfo.Symbol
                        ?? (symbolInfo.CandidateSymbols.Length == 1 ? symbolInfo.CandidateSymbols[0] : null);
                    if (referencedSymbol is null)
                    {
                        return null;
                    }
                    if (!SymbolEqualityComparer.Default.Equals(referencedSymbol.OriginalDefinition, method.OriginalDefinition))
                    {
                        continue;
                    }
                    if (solution.GetDocument(document.Id) is null
                        || !await AddEditableToken(
                            document, cref.Name.GetFirstToken().Span, method.Name, newName, allowedEdits, cancellationToken).ConfigureAwait(false))
                    {
                        return null;
                    }
                    targets.Add(cref, new SyntaxAnnotation());
                }
                if (targets.Count != 0)
                {
                    root = root.ReplaceNodes(targets.Keys, (original, rewritten) => rewritten.WithAdditionalAnnotations(targets[original]));
                    annotatedSolution = annotatedSolution.WithDocumentSyntaxRoot(document.Id, root);
                    annotations.Add(document.Id, targets.Values.ToList());
                }
            }
        }

        return (annotatedSolution, annotations);
    }

    private static bool IsEligible(IMethodSymbol method, SyntaxNode declaration, SemanticModel model, string newName)
    {
        if (method.IsVirtual || method.IsOverride || method.ExplicitInterfaceImplementations.Length != 0
            || method.ContainingType.TypeKind == TypeKind.Interface
            || method.PartialDefinitionPart is not null || method.PartialImplementationPart is not null
            || method.DeclaringSyntaxReferences.Length != 1
            || method.GetAttributes().Length != 0 || method.GetReturnTypeAttributes().Length != 0
            || method.Parameters.Any(parameter => parameter.GetAttributes().Length != 0)
            || model.LookupSymbols(declaration.SpanStart, name: newName).Length != 0)
        {
            return false;
        }

        if (method.MethodKind == MethodKind.LocalFunction)
        {
            return declaration.Parent is BlockSyntax
                && !model.LookupSymbols(declaration.SpanStart, name: method.Name)
                    .Any(symbol => !SymbolEqualityComparer.Default.Equals(symbol, method));
        }

        if (method.MethodKind != MethodKind.Ordinary
            || method.DeclaredAccessibility is not (Accessibility.Private or Accessibility.Public)
            || declaration is not MethodDeclarationSyntax syntax || syntax.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            return false;
        }

        for (var type = method.ContainingType; type is not null; type = type.BaseType)
        {
            if (type.Name == newName || type.GetMembers(newName).Length != 0
                || type.GetMembers(method.Name).Any(symbol => !SymbolEqualityComparer.Default.Equals(symbol, method)))
            {
                return false;
            }
        }

        foreach (var contract in method.ContainingType.AllInterfaces)
        {
            foreach (var member in contract.GetMembers(method.Name))
            {
                if (SymbolEqualityComparer.Default.Equals(method.ContainingType.FindImplementationForInterfaceMember(member), method))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static async Task<bool> AddEditableToken(
        Document document,
        TextSpan referenceSpan,
        string oldName,
        string newName,
        Dictionary<DocumentId, HashSet<TextSpan>> allowedEdits,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null
            || document.Name.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || document.Name.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase)
            || document.Name.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)
            || root.GetLeadingTrivia().ToFullString().IndexOf("<auto-generated", StringComparison.OrdinalIgnoreCase) >= 0
            || (document.FilePath is not null && document.Project.Solution.GetDocumentIdsWithFilePath(document.FilePath).Length != 1))
        {
            return false;
        }

        var token = root.FindToken(referenceSpan.Start, findInsideTrivia: true);
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (token.ValueText != oldName || !referenceSpan.Contains(token.Span)
            || model is null || model.LookupSymbols(token.SpanStart, name: newName).Length != 0)
        {
            return false;
        }

        if (!allowedEdits.TryGetValue(document.Id, out var spans))
        {
            spans = [];
            allowedEdits.Add(document.Id, spans);
        }
        spans.Add(token.Span);
        return true;
    }

    private static async Task<bool> HasUntrackedNameReference(Solution solution, string name, CancellationToken cancellationToken)
    {
        foreach (var project in solution.Projects)
        {
            foreach (var additionalDocument in project.AdditionalDocuments)
            {
                var text = await additionalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                if (text.ToString().IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            var generatedDocuments = await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);
            foreach (var document in project.Documents.Concat(generatedDocuments))
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (root is null || project.Language != LanguageNames.CSharp)
                {
                    var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    if (text.ToString().IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                    continue;
                }

                if (root.DescendantNodes().OfType<LiteralExpressionSyntax>()
                    .Any(literal => literal.Token.Value is string value && value.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    || root.DescendantNodes().OfType<InterpolatedStringTextSyntax>()
                        .Any(text => text.TextToken.ValueText.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return true;
                }

                var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                if (model is null)
                {
                    return true;
                }
                foreach (var reference in root.DescendantNodes().OfType<SimpleNameSyntax>()
                    .Where(reference => reference.Identifier.ValueText == name))
                {
                    if (reference.Parent is NameEqualsSyntax or NameColonSyntax)
                    {
                        continue;
                    }
                    var symbolInfo = model.GetSymbolInfo(reference, cancellationToken);
                    if (symbolInfo.Symbol is null && symbolInfo.CandidateSymbols.Length == 0
                        && model.GetMemberGroup(reference, cancellationToken).Length == 0)
                    {
                        return true;
                    }
                }

                foreach (var expression in root.DescendantNodes().OfType<ExpressionSyntax>()
                    .Where(expression => expression is BinaryExpressionSyntax or InterpolatedStringExpressionSyntax))
                {
                    var constant = model.GetConstantValue(expression, cancellationToken);
                    if (constant.HasValue && constant.Value is string value && value.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConditionalAsyncSuffixCodeFixProvider)), Shared]
public class ConditionalAsyncSuffixCodeFixProvider : CodeFixProvider
{
    private const string EquivalenceKey = "RemoveAsyncSuffix";

    public override ImmutableArray<string> FixableDiagnosticIds => ["DWARD0005"];

    public override FixAllProvider? GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var renamedSolution = await ConservativeAsyncRename.Create(
                context.Document, diagnostic.Location.SourceSpan, context.CancellationToken).ConfigureAwait(false);
            if (renamedSolution is null)
            {
                continue;
            }

            var action = CodeAction.Create(
                "Remove 'Async' suffix from declaration and references",
                _ => Task.FromResult(renamedSolution),
                equivalenceKey: EquivalenceKey);
            context.RegisterCodeFix(action, diagnostic);
        }
    }
}
