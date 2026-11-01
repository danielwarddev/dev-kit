using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Dward.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ConditionalAsyncSuffixAnalyzer : DiagnosticAnalyzer
{
    private const string DiagnosticId = "DWARD0005";
    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        "Use Async suffix in a member name only to distinguish from a synchronous counterpart",
        "Async declaration '{0}' has no matching synchronous counterpart '{1}'; remove the 'Async' suffix",
        "Naming",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Members that are async only need an Async suffix when there is a corresponding synchronous operation with the same signature, which helps differentiate them; you can tell it's async by looking at its signature. Overrides and interface implementations still retain contract names.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(nodeContext =>
        {
            var identifier = nodeContext.Node switch
            {
                MethodDeclarationSyntax method => method.Identifier,
                LocalFunctionStatementSyntax local => local.Identifier,
                _ => default
            };
            
            var methodSymbol = nodeContext.SemanticModel.GetDeclaredSymbol(nodeContext.Node, nodeContext.CancellationToken) as IMethodSymbol;
            if (methodSymbol is not { IsAsync: true } ||
                !methodSymbol.Name.EndsWith("Async", StringComparison.Ordinal) ||
                methodSymbol.IsOverride ||
                methodSymbol.ExplicitInterfaceImplementations.Length != 0)
            {
                return;
            }

            foreach (var contract in methodSymbol.ContainingType.AllInterfaces)
            {
                foreach (var member in contract.GetMembers(methodSymbol.Name))
                {
                    var implementation = methodSymbol.ContainingType.FindImplementationForInterfaceMember(member);
                    if (SymbolEqualityComparer.Default.Equals(implementation, methodSymbol))
                    {
                        return;
                    }
                }
            }

            var counterpartName = methodSymbol.Name.Substring(0, methodSymbol.Name.Length - "Async".Length);
            var matcher = new SynchronousCounterpartMatcher(nodeContext.Compilation);
            
            if (nodeContext.Node is LocalFunctionStatementSyntax localFunction)
            {
                if (localFunction.Parent is BlockSyntax block)
                {
                    foreach (var sibling in block.Statements.OfType<LocalFunctionStatementSyntax>())
                    {
                        var candidate = nodeContext.SemanticModel.GetDeclaredSymbol(sibling, nodeContext.CancellationToken);
                        if (candidate is not null && matcher.IsCounterpart(methodSymbol, candidate, counterpartName))
                        {
                            return;
                        }
                    }
                }
            }
            else
            {
                for (var type = methodSymbol.ContainingType; type is not null; type = type.BaseType)
                {
                    foreach (var candidate in type.GetMembers(counterpartName).OfType<IMethodSymbol>())
                    {
                        if (nodeContext.Compilation.IsSymbolAccessibleWithin(candidate, methodSymbol.ContainingType) &&
                            matcher.IsCounterpart(methodSymbol, candidate, counterpartName))
                        {
                            return;
                        }
                    }
                }
            }

            var diagnostic = Diagnostic.Create(_rule, identifier.GetLocation(), methodSymbol.Name, counterpartName);
            nodeContext.ReportDiagnostic(diagnostic);
        }, SyntaxKind.MethodDeclaration, SyntaxKind.LocalFunctionStatement);
    }
}
