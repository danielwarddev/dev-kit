using Dward.Analyzers.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Dward.Analyzers.UnitTests;

public class ExplicitSealedDeclarationCodeFixTests
{
    private readonly CSharpCodeFixTest<ExplicitSealedDeclarationAnalyzer, ExplicitSealedDeclarationCodeFixProvider, DefaultVerifier> _codeFixTester;

    public ExplicitSealedDeclarationCodeFixTests()
    {
        _codeFixTester = new()
        {
            ReferenceAssemblies = new ReferenceAssemblies("net10.0")
        };

        var referenceDirectory = Path.Combine(AppContext.BaseDirectory, "ReferenceAssemblies");
        var referencePaths = Directory.GetFiles(referenceDirectory, "*.dll");
        if (referencePaths.Length == 0)
        {
            throw new InvalidOperationException($"Missing test reference assemblies in '{referenceDirectory}'.");
        }

        foreach (var referencePath in referencePaths)
        {
            var reference = MetadataReference.CreateFromFile(referencePath);
            _codeFixTester.TestState.AdditionalReferences.Add(reference);
        }

        _codeFixTester.SolutionTransforms.Add((solution, projectId) =>
        {
            var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp12);
            return solution.WithProjectParseOptions(projectId, parseOptions);
        });
    }

    [Theory]
    [InlineData("class")]
    [InlineData("record")]
    [InlineData("record class")]
    public async Task When_Type_Is_Explicitly_Sealed_Then_Only_Modifier_Is_Removed(string declarationKind)
    {
        _codeFixTester.TestCode = $$"""
            public {|#0:sealed|} {{declarationKind}} Example
            {
            }
            """;
        _codeFixTester.FixedCode = $$"""
            public {{declarationKind}} Example
            {
            }
            """;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0004", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("Example"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("sealed public partial", "public partial")]
    [InlineData("public sealed partial", "public partial")]
    [InlineData("public sealed", "public")]
    [InlineData("public /* before */ sealed /* after */ partial", "public /* before */ /* after */ partial")]
    [InlineData("sealed /* after */ public partial", "/* after */ public partial")]
    [InlineData("public sealed // keep\npartial", "public // keep\npartial")]
    public async Task When_Modifier_Has_Trivia_Then_Comments_Attributes_And_Other_Modifiers_Are_Preserved(
        string modifiers,
        string fixedModifiers)
    {
        var markedModifiers = modifiers.Replace("sealed", "{|#0:sealed|}", StringComparison.Ordinal);
        _codeFixTester.TestCode = $$"""
            /// <summary>Keep documentation.</summary>
            [System.Obsolete]
            {{markedModifiers}} class Example<T> where T : class
            {
            }
            """;
        _codeFixTester.FixedCode = $$"""
            /// <summary>Keep documentation.</summary>
            [System.Obsolete]
            {{fixedModifiers}} class Example<T> where T : class
            {
            }
            """;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0004", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("Example"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Modifier_Follows_Directive_Then_Directive_And_Indentation_Are_Preserved()
    {
        _codeFixTester.TestCode = """
            #if true
            {|#0:sealed|} class Example
            {
                // Keep this body.
            }
            #endif
            """;
        _codeFixTester.FixedCode = """
            #if true
            class Example
            {
                // Keep this body.
            }
            #endif
            """;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0004", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("Example"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Sealed_Is_First_Nested_Modifier_Then_Leading_Trivia_Is_Preserved()
    {
        _codeFixTester.TestCode = """
            public class Container
            {
                /// <summary>Keep nested documentation.</summary>
                /* before */ {|#0:sealed|} /* after */ class Example
                {
                }
            }
            """;
        _codeFixTester.FixedCode = """
            public class Container
            {
                /// <summary>Keep nested documentation.</summary>
                /* before */ /* after */ class Example
                {
                }
            }
            """;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0004", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("Example"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Nested_And_Partial_Types_Are_Sealed_Then_Fix_All_Composes_Across_Documents()
    {
        _codeFixTester.TestState.Sources.Add(("First.cs", """
            public {|#0:sealed|} partial class Container
            {
                private {|#1:sealed|} class @Example<T>
                {
                }

                public {|#2:sealed|} record Entry(int Value);
                public {|#3:sealed|} record class OtherEntry(int Value);
            }
            """));
        _codeFixTester.TestState.Sources.Add(("Second.cs", """
            public {|#4:sealed|} partial class Container
            {
            }
            """));
        _codeFixTester.FixedState.Sources.Add(("First.cs", """
            public partial class Container
            {
                private class @Example<T>
                {
                }

                public record Entry(int Value);
                public record class OtherEntry(int Value);
            }
            """));
        _codeFixTester.FixedState.Sources.Add(("Second.cs", """
            public partial class Container
            {
            }
            """));
        string[] names = ["Container", "Example", "Entry", "OtherEntry", "Container"];
        for (var index = 0; index < names.Length; index++)
        {
            _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0004", DiagnosticSeverity.Warning)
                .WithLocation(index).WithArguments(names[index]));
        }

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Other_Sealed_Declarations_Are_Excluded_Then_Fix_Leaves_Them_Unchanged()
    {
        const string generatedCode = """
            // <auto-generated/>
            public sealed class Generated
            {
            }
            """;
        _codeFixTester.TestCode = """
            public class Base
            {
                public virtual int Value => 1;
                public virtual void Execute() { }
            }

            public {|#0:sealed|} class Derived : Base
            {
                public sealed override int Value => 2;
                public sealed override void Execute() { }
            }

            public static class StaticExample { }
            public readonly record struct Entry(int Value);
            """;
        _codeFixTester.TestState.Sources.Add(("Generated.g.cs", generatedCode));
        _codeFixTester.FixedCode = """
            public class Base
            {
                public virtual int Value => 1;
                public virtual void Execute() { }
            }

            public class Derived : Base
            {
                public sealed override int Value => 2;
                public sealed override void Execute() { }
            }

            public static class StaticExample { }
            public readonly record struct Entry(int Value);
            """;
        _codeFixTester.FixedState.Sources.Add(("Generated.g.cs", generatedCode));
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0004", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("Derived"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }
}
