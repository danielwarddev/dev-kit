using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Dward.Godot.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class GodotFreeProjectAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor _rule = new(
        "DWARDGD0002",
        "Keep opted-in projects Godot-free",
        "Godot-free project references prohibited assembly '{0}'. Keep game rules portable and testable without Godot; remove the direct or transitive dependency and move Godot-dependent code, including pure helpers using Godot types, into the Godot-facing layer.",
        "Architecture",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The dependency boundary prevents engine coupling from reaching core game rules, even through unused or transitive references. Keep engine-independent rules in the core and Godot-specific adapters and helpers in the Godot-facing layer, which may reference the core, not the reverse.",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationAction(compilationContext =>
        {
            var options = compilationContext.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
            
            var hasGodotFreeProjectOption = options.TryGetValue(
                "build_property.GodotFreeProject", out var godotFreeProjectValue);
            var godotFreeProjectOptionIsTrue = string.Equals(
                godotFreeProjectValue, "true", StringComparison.OrdinalIgnoreCase);

            if (!hasGodotFreeProjectOption || !godotFreeProjectOptionIsTrue)
            {
                return;
            }

            foreach (var reference in compilationContext.Compilation.References)
            {
                var referencedAssemblyOrModule = compilationContext.Compilation.GetAssemblyOrModuleSymbol(reference);

                if (referencedAssemblyOrModule is not IAssemblySymbol referencedAssembly)
                {
                    continue;
                }

                var isGodotSharp = string.Equals(
                    referencedAssembly.Name, "GodotSharp", StringComparison.OrdinalIgnoreCase);
                var isGodotSharpEditor = string.Equals(
                    referencedAssembly.Name, "GodotSharpEditor", StringComparison.OrdinalIgnoreCase);

                if (!isGodotSharp && !isGodotSharpEditor)
                {
                    continue;
                }

                var diagnostic = Diagnostic.Create(_rule, Location.None, referencedAssembly.Name);
                compilationContext.ReportDiagnostic(diagnostic);
            }
        });
    }
}
