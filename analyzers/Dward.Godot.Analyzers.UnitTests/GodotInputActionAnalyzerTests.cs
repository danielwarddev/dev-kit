using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Dward.Godot.Analyzers.UnitTests;

public class GodotInputActionAnalyzerTests
{
    private readonly CSharpAnalyzerTest<GodotInputActionAnalyzer, DefaultVerifier> _analyzerTester;

    public GodotInputActionAnalyzerTests()
    {
        _analyzerTester = new CSharpAnalyzerTest<GodotInputActionAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = new ReferenceAssemblies("net10.0")
        };
        var referenceDirectory = Path.Combine(AppContext.BaseDirectory, "ReferenceAssemblies");
        var referencePaths = Directory.GetFiles(referenceDirectory, "*.dll");
        if (referencePaths.Length == 0)
        {
            throw new InvalidOperationException($"Missing test reference assemblies in '{referenceDirectory}'.");
        }

        foreach (var path in referencePaths)
        {
            _analyzerTester.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(path));
        }

        _analyzerTester.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(global::Godot.Input).Assembly.Location));
        _analyzerTester.TestState.Sources.Add(("Baseline.cs", """
            public class Baseline
            {
            }
            """));
        _analyzerTester.SolutionTransforms.Add((solution, projectId) =>
            solution.WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.CSharp12)));
        _analyzerTester.TestState.AdditionalFiles.Add(("project.godot", """
            config_version=5

            [input]

            jump={
            "deadzone": 0.5,
            "events": []
            }
            "move left"={
            "events": [Object(InputEventKey,"resource_name":"[input]", "pressed":false)]
            }

            [application]

            config/name="Example"
            """));
    }

    [Fact]
    public async Task When_Literals_Constants_And_Concatenations_Are_Unknown_Then_Arguments_Are_Reported()
    {
        _analyzerTester.TestCode = """
            using InputAlias = Godot.Input;
            using static Godot.Input;

            public class Example
            {
                public void CheckActions()
                {
                    const string action = "missing";
                    InputAlias.IsActionPressed({|#0:"misspelled"|});
                    IsActionJustPressed({|#1:action|});
                    Godot.Input.ActionPress({|#2:"not" + "_declared"|});
                    Godot.Input.ActionRelease(new Godot.StringName({|#3:"absent"|}));
                }
            }
            """;
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0003", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("misspelled"));
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0003", DiagnosticSeverity.Warning)
            .WithLocation(1).WithArguments("missing"));
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0003", DiagnosticSeverity.Warning)
            .WithLocation(2).WithArguments("not_declared"));
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0003", DiagnosticSeverity.Warning)
            .WithLocation(3).WithArguments("absent"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Vector_And_Axis_Use_Named_Arguments_Then_All_Action_Positions_Are_Checked()
    {
        _analyzerTester.TestCode = """
            public class Example
            {
                public void CheckActions()
                {
                    Godot.Input.GetVector(positiveY: {|#0:"down"|}, negativeX: "jump",
                        positiveX: {|#1:"right"|}, negativeY: "ui_up", deadzone: 0.5f);
                    Godot.Input.GetAxis(positiveAction: "jump", negativeAction: {|#2:"left"|});
                }
            }
            """;
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0003", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("down"));
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0003", DiagnosticSeverity.Warning)
            .WithLocation(1).WithArguments("right"));
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0003", DiagnosticSeverity.Warning)
            .WithLocation(2).WithArguments("left"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Actions_Are_Declared_Built_In_Or_Computed_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestCode = """
            public class Example
            {
                public void CheckActions(string dynamicAction)
                {
                    const string action = "jump";
                    Godot.Input.IsActionPressed(action);
                    Godot.Input.IsActionJustReleased("move left");
                    Godot.Input.GetActionStrength("ui_accept");
                    Godot.Input.GetActionRawStrength("ui_text_backspace");
                    Godot.Input.IsActionPressed("ui_menu");
                    Godot.Input.IsActionPressed("ui_text_completion_accept");
                    Godot.Input.IsActionPressed("ui_text_completion_replace");
                    Godot.Input.ActionPress(dynamicAction);
                    Godot.Input.ActionRelease(new Godot.StringName("jump"));
                }
            }
            """;

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("IsActionPressed")]
    [InlineData("IsActionJustPressed")]
    [InlineData("IsActionJustReleased")]
    [InlineData("GetActionStrength")]
    [InlineData("GetActionRawStrength")]
    [InlineData("ActionPress")]
    [InlineData("ActionRelease")]
    public async Task When_Single_Action_API_Uses_Unknown_Action_Then_Diagnostic_Is_Reported(string method)
    {
        _analyzerTester.TestCode = $$"""
            public class Example
            {
                public void CheckActions()
                {
                    Godot.Input.{{method}}({|#0:"ui_typo"|});
                }
            }
            """;
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0003", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("ui_typo"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_AdditionalFile_Is_Not_Supplied_Then_Project_Is_Not_Opted_In()
    {
        _analyzerTester.TestState.AdditionalFiles.Clear();
        _analyzerTester.TestCode = """
            public class Example
            {
                public void CheckActions()
                {
                    Godot.Input.IsActionPressed("missing");
                }
            }
            """;

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Input_Name_Is_Unrelated_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestCode = """
            public class Input
            {
                public static bool IsActionPressed(string action) => false;
            }

            public class Example
            {
                public void CheckActions()
                {
                    Input.IsActionPressed("missing");
                }
            }
            """;

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Godot_Is_Absent_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestState.AdditionalReferences.RemoveAt(_analyzerTester.TestState.AdditionalReferences.Count - 1);
        _analyzerTester.TestCode = """
            namespace Godot
            {
                public class Input
                {
                    public static bool IsActionPressed(string action) => false;
                }
            }

            public class Example
            {
                public void CheckActions()
                {
                    Godot.Input.IsActionPressed("missing");
                }
            }
            """;

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Input_Call_Is_Generated_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestState.Sources.Add(("Example.g.cs", """
            // <auto-generated/>
            public class Example
            {
                public void CheckActions()
                {
                    Godot.Input.IsActionPressed("missing");
                }
            }
            """));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Rule_Is_Disabled_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestCode = """
            public class Example
            {
                public void CheckActions()
                {
                    Godot.Input.IsActionPressed("missing");
                }
            }
            """;
        _analyzerTester.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", """
            is_global = true
            dotnet_diagnostic.DWARDGD0003.severity = none
            """));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("""
        [input]
        broken={
        """)]
    [InlineData("""
        [input]
        broken=[]
        """)]
    [InlineData("""
        [input]
        broken={"events": [)}
        """)]
    public async Task When_Input_Configuration_Is_Malformed_Then_Configuration_Diagnostic_Is_Reported(string configuration)
    {
        _analyzerTester.TestState.AdditionalFiles.Clear();
        _analyzerTester.TestState.AdditionalFiles.Add(("project.godot", configuration));
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0004", DiagnosticSeverity.Warning)
            .WithNoLocation().WithArguments("the [input] section is malformed"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Multiple_Projects_Are_Supplied_Then_Configuration_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestState.AdditionalFiles.Add(("nested/project.godot", """
            config_version=5
            """));
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0004", DiagnosticSeverity.Warning)
            .WithNoLocation().WithArguments("multiple project.godot AdditionalFiles were supplied"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }
}
