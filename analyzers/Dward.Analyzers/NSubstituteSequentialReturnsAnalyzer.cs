using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Dward.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NSubstituteSequentialReturnsAnalyzer : DiagnosticAnalyzer
{
    private const string DiagnosticId = "DWARD0003";
    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        "Use a typed empty sequential return value",
        "Bare '[]' supplies no additional return values to NSubstitute.Returns; assign the empty '{0}' value to a typed local first",
        "Correctness",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "An empty params array is not an empty collection returned on the next call. Use a typed empty value, or omit the argument if no additional returns are intended.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var extensions = compilationContext.Compilation.GetTypeByMetadataName("NSubstitute.SubstituteExtensions");
            if (extensions is null || extensions.ContainingAssembly.Identity.Name != "NSubstitute")
            {
                return;
            }

            var enumerable = compilationContext.Compilation.GetTypeByMetadataName("System.Collections.IEnumerable");
            compilationContext.RegisterOperationAction(operationContext =>
            {
                var invocation = (IInvocationOperation)operationContext.Operation;
                var method = invocation.TargetMethod;
                var definition = method.OriginalDefinition;
                
                if (method.Name != "Returns" ||
                    !SymbolEqualityComparer.Default.Equals(method.ContainingType, extensions) ||
                    !definition.IsExtensionMethod ||
                    definition.TypeParameters.Length != 1 ||
                    definition.Parameters.Length != 3 ||
                    !SymbolEqualityComparer.Default.Equals(definition.Parameters[1].Type, definition.TypeParameters[0]) ||
                    definition.Parameters[2] is not { IsParams: true, Type: IArrayTypeSymbol paramsType } ||
                    !SymbolEqualityComparer.Default.Equals(paramsType.ElementType, definition.TypeParameters[0]))
                {
                    return;
                }

                var returnType = method.TypeArguments[0];
                if (returnType.SpecialType == SpecialType.System_String ||
                    (returnType is not IArrayTypeSymbol &&
                     !returnType.AllInterfaces.Any(type => SymbolEqualityComparer.Default.Equals(type, enumerable)) &&
                     !SymbolEqualityComparer.Default.Equals(returnType, enumerable) &&
                     !returnType.GetAttributes().Any(attribute =>
                         attribute.AttributeClass?.ToDisplayString() == "System.Runtime.CompilerServices.CollectionBuilderAttribute")))
                {
                    return;
                }

                foreach (var argument in invocation.Arguments)
                {
                    if (argument.Parameter?.Ordinal == 2 &&
                        argument.ArgumentKind == ArgumentKind.Explicit &&
                        argument.Syntax is ArgumentSyntax { Expression: CollectionExpressionSyntax { Elements.Count: 0 } empty })
                    {
                        var diagnostic = Diagnostic.Create(_rule, empty.GetLocation(), returnType.ToDisplayString());
                        operationContext.ReportDiagnostic(diagnostic);
                    }
                }
            }, OperationKind.Invocation);
        });
    }
}
