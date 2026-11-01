using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Dward.Godot.Analyzers.UnitTests;

public class PrivateGodotExportAnalyzerTests
{
    private readonly CSharpAnalyzerTest<PrivateGodotExportAnalyzer, DefaultVerifier> _analyzerTester;

    public PrivateGodotExportAnalyzerTests()
    {
        _analyzerTester = new CSharpAnalyzerTest<PrivateGodotExportAnalyzer, DefaultVerifier>
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

        _analyzerTester.TestState.Sources.Add(("GodotAttribute.cs", """
            namespace Godot
            {
                [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
                public class ExportAttribute : System.Attribute { }
            }
            """));
    }

    [Theory]
    [InlineData("public")]
    [InlineData("protected")]
    [InlineData("internal")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    public async Task When_Export_Is_Not_Private_Then_Field_And_Property_Are_Reported(string accessibility)
    {
        _analyzerTester.TestCode = $$"""
            public partial class ExampleNode
            {
                [Godot.Export]
                {{accessibility}} int {|#0:Count|};

                [Godot.Export]
                {{accessibility}} int {|#1:Value|} { get; set; }
            }
            """;

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0001", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("Count"));
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0001", DiagnosticSeverity.Warning)
            .WithLocation(1).WithArguments("Value"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Exports_Are_Private_Then_No_Diagnostics_Are_Reported()
    {
        _analyzerTester.TestCode = """
            public class ExampleNode
            {
                [Godot.Export]
                private int _count;

                [Godot.Export]
                int _implicitPrivateCount;

                [Godot.Export]
                private int Value { get; set; }

                public int Count => _count + _implicitPrivateCount + Value;
            }
            """;

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Aliased_Exports_Appear_In_Partial_Class_Then_Each_Member_Is_Reported_Once()
    {
        _analyzerTester.TestState.Sources.Add(("First.cs", """
            using ExportAlias = Godot.ExportAttribute;

            public partial class ExampleNode
            {
                [ExportAlias]
                public int {|#0:First|}, {|#1:Second|};
            }
            """));
        _analyzerTester.TestState.Sources.Add(("Second.cs", """
            public partial class ExampleNode
            {
                [global::Godot.ExportAttribute]
                public int {|#2:Value|} { get; set; }
            }
            """));

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0001", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("First"));
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0001", DiagnosticSeverity.Warning)
            .WithLocation(1).WithArguments("Second"));
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0001", DiagnosticSeverity.Warning)
            .WithLocation(2).WithArguments("Value"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Same_Named_Attribute_Is_Unrelated_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestCode = """
            namespace Other
            {
                public class ExportAttribute : System.Attribute { }
            }

            public class ExampleNode
            {
                [Other.Export]
                public int Count;
            }
            """;

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Godot_Is_Absent_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestState.Sources.Clear();

        _analyzerTester.TestCode = """
            public class ExportAttribute : System.Attribute { }

            public class ExampleNode
            {
                [Export]
                public int Count;
            }
            """;

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Export_Is_In_Generated_Code_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestState.Sources.Add(("ExampleNode.g.cs", """
            // <auto-generated/>
            public class ExampleNode
            {
                [Godot.Export]
                public int Count;
            }
            """));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Rule_Is_Disabled_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestCode = """
            public class ExampleNode
            {
                [Godot.Export]
                public int Count;
            }
            """;
        _analyzerTester.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", """
            is_global = true
            dotnet_diagnostic.DWARDGD0001.severity = none
            """));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }
}
