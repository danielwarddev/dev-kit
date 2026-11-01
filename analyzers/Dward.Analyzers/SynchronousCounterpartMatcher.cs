using Microsoft.CodeAnalysis;

namespace Dward.Analyzers;

public class SynchronousCounterpartMatcher(Compilation compilation)
{
    public bool IsCounterpart(IMethodSymbol asyncMethod, IMethodSymbol candidate, string counterpartName)
    {
        if (candidate.Name != counterpartName ||
            candidate.MethodKind != asyncMethod.MethodKind ||
            candidate.IsAsync ||
            candidate.Arity != asyncMethod.Arity ||
            candidate.IsStatic != asyncMethod.IsStatic ||
            candidate.IsExtensionMethod != asyncMethod.IsExtensionMethod ||
            candidate.DeclaredAccessibility != asyncMethod.DeclaredAccessibility ||
            candidate.ReturnsByRef || candidate.ReturnsByRefReadonly ||
            IsTaskLike(candidate.ReturnType))
        {
            return false;
        }

        var parameterCount = asyncMethod.Parameters.Length;
        if (parameterCount == candidate.Parameters.Length + 1)
        {
            var lastParameter = asyncMethod.Parameters[parameterCount - 1];
            var cancellationToken = compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
            if (lastParameter.RefKind != RefKind.None ||
                !SymbolEqualityComparer.Default.Equals(lastParameter.Type, cancellationToken))
            {
                return false;
            }

            parameterCount--;
        }

        if (parameterCount != candidate.Parameters.Length)
        {
            return false;
        }

        for (var index = 0; index < parameterCount; index++)
        {
            var asyncParameter = asyncMethod.Parameters[index];
            var syncParameter = candidate.Parameters[index];
            if (asyncParameter.RefKind != syncParameter.RefKind ||
                asyncParameter.IsParams != syncParameter.IsParams ||
                !CorrespondingTypes(asyncParameter.Type, syncParameter.Type, asyncMethod, candidate))
            {
                return false;
            }
        }

        return CorrespondingReturnType(asyncMethod, candidate);
    }

    private bool CorrespondingReturnType(IMethodSymbol asyncMethod, IMethodSymbol candidate)
    {
        if (asyncMethod.ReturnsVoid)
        {
            return candidate.ReturnsVoid;
        }

        if (asyncMethod.ReturnType is INamedTypeSymbol returnType)
        {
            var definition = returnType.OriginalDefinition;
            if (SymbolEqualityComparer.Default.Equals(definition, compilation.GetTypeByMetadataName("System.Threading.Tasks.Task")) ||
                SymbolEqualityComparer.Default.Equals(definition, compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask")))
            {
                return candidate.ReturnsVoid;
            }

            if (SymbolEqualityComparer.Default.Equals(definition, compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1")) ||
                SymbolEqualityComparer.Default.Equals(definition, compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1")))
            {
                return CorrespondingTypes(returnType.TypeArguments[0], candidate.ReturnType, asyncMethod, candidate);
            }

            var syncStreamMetadataName =
                SymbolEqualityComparer.Default.Equals(definition, compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1"))
                    ? "System.Collections.Generic.IEnumerable`1"
                    : SymbolEqualityComparer.Default.Equals(definition, compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerator`1"))
                        ? "System.Collections.Generic.IEnumerator`1"
                        : null;
            if (syncStreamMetadataName is not null)
            {
                var syncStreamDefinition = compilation.GetTypeByMetadataName(syncStreamMetadataName);
                return candidate.ReturnType is INamedTypeSymbol syncReturn &&
                    SymbolEqualityComparer.Default.Equals(syncReturn.OriginalDefinition, syncStreamDefinition) &&
                    CorrespondingTypes(returnType.TypeArguments[0], syncReturn.TypeArguments[0], asyncMethod, candidate);
            }
        }

        return true;
    }

    private bool IsTaskLike(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType)
        {
            foreach (var metadataName in new[]
            {
                "System.Threading.Tasks.Task", "System.Threading.Tasks.Task`1",
                "System.Threading.Tasks.ValueTask", "System.Threading.Tasks.ValueTask`1",
                "System.Collections.Generic.IAsyncEnumerable`1", "System.Collections.Generic.IAsyncEnumerator`1"
            })
            {
                if (SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, compilation.GetTypeByMetadataName(metadataName)))
                {
                    return true;
                }
            }
        }

        var builderAttribute = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.AsyncMethodBuilderAttribute");
        return type.GetAttributes().Any(attribute =>
            SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, builderAttribute));
    }

    private static bool CorrespondingTypes(ITypeSymbol first, ITypeSymbol second, IMethodSymbol firstMethod, IMethodSymbol secondMethod)
    {
        if (SymbolEqualityComparer.Default.Equals(first, second))
        {
            return true;
        }

        if (first is ITypeParameterSymbol firstParameter && second is ITypeParameterSymbol secondParameter)
        {
            return firstParameter.Ordinal == secondParameter.Ordinal &&
                SymbolEqualityComparer.Default.Equals(firstParameter.ContainingSymbol, firstMethod) &&
                SymbolEqualityComparer.Default.Equals(secondParameter.ContainingSymbol, secondMethod);
        }

        if (first is IArrayTypeSymbol firstArray && second is IArrayTypeSymbol secondArray)
        {
            return firstArray.Rank == secondArray.Rank &&
                CorrespondingTypes(firstArray.ElementType, secondArray.ElementType, firstMethod, secondMethod);
        }

        if (first is INamedTypeSymbol firstNamed && second is INamedTypeSymbol secondNamed &&
            SymbolEqualityComparer.Default.Equals(firstNamed.OriginalDefinition, secondNamed.OriginalDefinition))
        {
            if (firstNamed.ContainingType is not null && secondNamed.ContainingType is not null &&
                !CorrespondingTypes(firstNamed.ContainingType, secondNamed.ContainingType, firstMethod, secondMethod))
            {
                return false;
            }

            for (var index = 0; index < firstNamed.TypeArguments.Length; index++)
            {
                if (!CorrespondingTypes(firstNamed.TypeArguments[index], secondNamed.TypeArguments[index], firstMethod, secondMethod))
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }
}
