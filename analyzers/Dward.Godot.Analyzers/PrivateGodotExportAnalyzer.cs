using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Dward.Godot.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PrivateGodotExportAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor _rule = new(
        "DWARDGD0001",
        "Godot [Export]s must be private",
        "Exported member '{0}' must be private; expose a separate get-only property when external reads are needed",
        "Encapsulation",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Exported Godot members are configuration that belong to their Node, and so must be private. If callers need to read these fields, add a get-only property that returns them.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var exportAttribute = compilationContext.Compilation.GetTypeByMetadataName("Godot.ExportAttribute");
            if (exportAttribute is null)
            {
                return;
            }

            compilationContext.RegisterSymbolAction(symbolContext =>
            {
                var member = symbolContext.Symbol;
                if (member.DeclaredAccessibility == Accessibility.Private)
                {
                    return;
                }

                foreach (var attribute in member.GetAttributes())
                {
                    if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, exportAttribute))
                    {
                        continue;
                    }

                    foreach (var location in member.Locations)
                    {
                        if (location.IsInSource)
                        {
                            var diagnostic = Diagnostic.Create(_rule, location, member.Name);
                            symbolContext.ReportDiagnostic(diagnostic);
                            break;
                        }
                    }

                    break;
                }
            }, SymbolKind.Field, SymbolKind.Property);
        });
    }
}
