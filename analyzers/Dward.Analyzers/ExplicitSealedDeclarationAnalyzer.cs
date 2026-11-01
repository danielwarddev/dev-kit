using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Dward.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ExplicitSealedDeclarationAnalyzer : DiagnosticAnalyzer
{
    private const string DiagnosticId = "DWARD0004";
    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        "Do not explicitly seal classes or record classes",
        "Type '{0}' must not declare an explicit 'sealed' modifier",
        "Design",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Don't use 'sealed'. Remove only the explicit sealed modifier from the class or record class; retain its other modifiers and behavior.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(nodeContext =>
        {
            var declaration = (TypeDeclarationSyntax)nodeContext.Node;
            if (declaration.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                return;
            }

            foreach (var modifier in declaration.Modifiers)
            {
                if (modifier.IsKind(SyntaxKind.SealedKeyword))
                {
                    var diagnostic = Diagnostic.Create(_rule, modifier.GetLocation(), declaration.Identifier.ValueText);
                    nodeContext.ReportDiagnostic(diagnostic);
                    break;
                }
            }
        }, SyntaxKind.ClassDeclaration, SyntaxKind.RecordDeclaration);
    }
}
