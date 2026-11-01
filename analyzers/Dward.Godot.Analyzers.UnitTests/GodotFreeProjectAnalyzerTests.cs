using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Dward.Godot.Analyzers.UnitTests;

public class GodotFreeProjectAnalyzerTests
{
    private readonly CSharpAnalyzerTest<GodotFreeProjectAnalyzer, DefaultVerifier> _analyzerTester;

    public GodotFreeProjectAnalyzerTests()
    {
        _analyzerTester = new CSharpAnalyzerTest<GodotFreeProjectAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = new ReferenceAssemblies("net10.0")
        };

        var referenceDirectory = Path.Combine(AppContext.BaseDirectory, "ReferenceAssemblies");
        var referencePaths = Directory.GetFiles(referenceDirectory, "*.dll");

        foreach (var path in referencePaths)
        {
            var reference = MetadataReference.CreateFromFile(path);
            _analyzerTester.TestState.AdditionalReferences.Add(reference);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("using Alias = Godot.Node;")]
    [InlineData("using Godot;")]
    [InlineData("using Alias = Godot.Node;\npublic class Derived : Alias { }")]
    [InlineData("public class Derived : Godot.Node { }")]
    [InlineData("public class Methods { public void Invoke(Godot.Node node) { node.QueueFree(); } }")]
    [InlineData("public class Exports { [Godot.Export] public int Count { get; set; } }")]
    public async Task When_Opted_In_Project_References_Godot_Then_Dependency_Is_Reported_Even_Without_Usage(
        string imports)
    {
        var reference = MetadataReference.CreateFromFile(typeof(global::Godot.Node).Assembly.Location);
        _analyzerTester.TestState.AdditionalReferences.Add(reference);
        _analyzerTester.TestCode = $$"""
            {{imports}}

            public class Example { }
            """;
        _analyzerTester.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", """
            is_global = true
            build_property.GodotFreeProject = true
            """));

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0002", DiagnosticSeverity.Warning)
            .WithNoLocation().WithArguments("GodotSharp"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Project_Is_Not_Opted_In_Then_Godot_Reference_Is_Allowed()
    {
        var reference = MetadataReference.CreateFromFile(typeof(global::Godot.Node).Assembly.Location);
        _analyzerTester.TestState.AdditionalReferences.Add(reference);
        _analyzerTester.TestCode = """
            public class Example { }
            """;

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Opted_In_Project_Defines_Unrelated_Godot_Names_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestCode = """
            namespace Godot
            {
                public class Node { }
            }

            public class Example : Godot.Node { }
            """;
        _analyzerTester.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", """
            is_global = true
            build_property.GodotFreeProject = true
            """));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }
}
