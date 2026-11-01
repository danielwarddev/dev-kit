using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Dward.Analyzers.UnitTests;

public class ExplicitSealedDeclarationAnalyzerTests
{
    private readonly CSharpAnalyzerTest<ExplicitSealedDeclarationAnalyzer, DefaultVerifier> _analyzerTester;

    public ExplicitSealedDeclarationAnalyzerTests()
    {
        _analyzerTester = new CSharpAnalyzerTest<ExplicitSealedDeclarationAnalyzer, DefaultVerifier>
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
            _analyzerTester.TestState.AdditionalReferences.Add(reference);
        }

        _analyzerTester.SolutionTransforms.Add((solution, projectId) =>
        {
            var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp12);
            return solution.WithProjectParseOptions(projectId, parseOptions);
        });
    }

    [Theory]
    [InlineData("class")]
    [InlineData("record")]
    [InlineData("record class")]
    public async Task When_Type_Is_Explicitly_Sealed_Then_Modifier_Is_Reported(string declarationKind)
    {
        _analyzerTester.TestCode = $$"""
            public {|#0:sealed|} {{declarationKind}} Example
            {
            }
            """;

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0004", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("Example"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Nested_Generic_And_Positional_Types_Are_Sealed_Then_Each_Modifier_Is_Reported()
    {
        _analyzerTester.TestCode = """
            public class Container
            {
                private {|#0:sealed|} class @Example<T>
                {
                }

                public {|#1:sealed|} record Entry(int Value);

                public {|#2:sealed|} record class OtherEntry(int Value);
            }
            """;

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0004", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("Example"));
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0004", DiagnosticSeverity.Warning)
            .WithLocation(1).WithArguments("Entry"));
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0004", DiagnosticSeverity.Warning)
            .WithLocation(2).WithArguments("OtherEntry"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("record")]
    [InlineData("record class")]
    public async Task When_One_Partial_Declaration_Is_Sealed_Then_Only_Explicit_Modifier_Is_Reported(string declarationKind)
    {
        _analyzerTester.TestState.Sources.Add(("First.cs", $$"""
            public {|#0:sealed|} partial {{declarationKind}} Example
            {
            }
            """));
        _analyzerTester.TestState.Sources.Add(("Second.cs", $$"""
            public partial {{declarationKind}} Example
            {
            }
            """));

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0004", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("Example"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Both_Partial_Declarations_Explicitly_Seal_Type_Then_Each_Modifier_Is_Reported_Once()
    {
        _analyzerTester.TestState.Sources.Add(("First.cs", """
            public {|#0:sealed|} partial class Placeholder
            {
            }
            """));
        _analyzerTester.TestState.Sources.Add(("Second.cs", """
            public {|#1:sealed|} partial class Placeholder
            {
            }
            """));

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0004", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("Placeholder"));
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0004", DiagnosticSeverity.Warning)
            .WithLocation(1).WithArguments("Placeholder"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Types_Are_Unsealed_Or_Sealed_By_Compiler_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestCode = """
            public class Example
            {
                public object Value => new { Count = 1 };
            }

            public abstract class AbstractExample
            {
            }

            public static class StaticExample
            {
            }

            public record Entry(int Value);
            public record class OtherEntry;
            public struct ValueEntry;
            public readonly struct ReadonlyEntry;
            public record struct RecordValueEntry(int Value);
            public readonly record struct ReadonlyRecordEntry(int Value);
            public delegate void Callback();
            public enum Choice { First }
            """;

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Overrides_Are_Sealed_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestCode = """
            public class Base
            {
                public virtual int Value => 1;

                public virtual void Execute()
                {
                }
            }

            public class Derived : Base
            {
                public sealed override int Value => 2;

                public sealed override void Execute()
                {
                }
            }
            """;

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Sealed_Modifier_Is_In_Generated_Partial_Then_User_Declaration_Is_Not_Reported()
    {
        _analyzerTester.TestState.Sources.Add(("Example.g.cs", """
            // <auto-generated/>
            public sealed partial class Example
            {
            }
            """));
        _analyzerTester.TestCode = """
            public partial class Example
            {
            }
            """;

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Rule_Is_Disabled_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestCode = """
            public sealed class Example
            {
            }
            """;
        _analyzerTester.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", """
            is_global = true
            dotnet_diagnostic.DWARD0004.severity = none
            """));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }
}
