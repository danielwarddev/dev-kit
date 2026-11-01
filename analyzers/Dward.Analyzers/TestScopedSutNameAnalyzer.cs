using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Dward.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TestScopedSutNameAnalyzer : DiagnosticAnalyzer
{
    private const string DiagnosticId = "DWARD0006";
    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        "Use descriptive variable names in test-scoped code",
        "Declaration '{0}' must use a descriptive name instead of 'sut' or '_sut'",
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "Rename the declaration and its resolved references to describe the tested subject or role. For instance, orderPricingService instead of sut,.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(nodeContext =>
            {
                var identifier = nodeContext.Node switch
                {
                    VariableDeclaratorSyntax variable => variable.Identifier,
                    ParameterSyntax parameter => parameter.Identifier,
                    PropertyDeclarationSyntax property => property.Identifier,
                    SingleVariableDesignationSyntax designation => designation.Identifier,
                    ForEachStatementSyntax loop => loop.Identifier,
                    CatchDeclarationSyntax exception => exception.Identifier,
                    FromClauseSyntax from => from.Identifier,
                    LetClauseSyntax let => let.Identifier,
                    JoinClauseSyntax join => join.Identifier,
                    JoinIntoClauseSyntax joinInto => joinInto.Identifier,
                    QueryContinuationSyntax continuation => continuation.Identifier,
                    _ => default
                };
                if (!string.Equals(identifier.ValueText, "sut", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(identifier.ValueText, "_sut", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // Event declarations also use variable declarators, but are outside this variable-name policy.
                if (nodeContext.Node is VariableDeclaratorSyntax { Parent.Parent: EventFieldDeclarationSyntax })
                {
                    return;
                }

                var diagnostic = Diagnostic.Create(_rule, identifier.GetLocation(), identifier.ValueText);
                nodeContext.ReportDiagnostic(diagnostic);
            },
                SyntaxKind.VariableDeclarator, SyntaxKind.Parameter, SyntaxKind.PropertyDeclaration,
                SyntaxKind.SingleVariableDesignation, SyntaxKind.ForEachStatement, SyntaxKind.CatchDeclaration,
                SyntaxKind.FromClause, SyntaxKind.LetClause, SyntaxKind.JoinClause,
                SyntaxKind.JoinIntoClause, SyntaxKind.QueryContinuation);
    }
}
