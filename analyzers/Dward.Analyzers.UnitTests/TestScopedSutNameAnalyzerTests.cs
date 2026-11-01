using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Dward.Analyzers.UnitTests;

public class TestScopedSutNameAnalyzerTests
{
    private readonly CSharpAnalyzerTest<TestScopedSutNameAnalyzer, DefaultVerifier> _analyzerTester;

    public TestScopedSutNameAnalyzerTests()
    {
        _analyzerTester = new CSharpAnalyzerTest<TestScopedSutNameAnalyzer, DefaultVerifier>
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
        _analyzerTester.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", """
            is_global = true
            dotnet_diagnostic.DWARD0006.severity = warning
            """));
    }

    [Theory]
    [InlineData("sut")]
    [InlineData("_sut")]
    [InlineData("Sut")]
    [InlineData("SUT")]
    [InlineData("_Sut")]
    [InlineData("_SUT")]
    public async Task When_Forbidden_Name_Is_Declared_Then_Exact_Identifier_Is_Reported(string name)
    {
        _analyzerTester.TestCode = $$"""
            public class Example
            {
                public void Configure()
                {
                    int {|#0:{{name}}|} = 1;
                }
            }
            """;

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0006", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments(name));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Field_Property_And_Parameters_Are_Forbidden_Then_Declarations_Not_Uses_Are_Reported()
    {
        _analyzerTester.TestCode = """
            public class Example
            {
                private int {|#0:_sut|}, _value;
                public int {|#1:Sut|} { get; set; }

                public int Configure(int {|#2:sut|})
                {
                    _sut = sut;
                    Sut = _sut;
                    System.Func<int, int> transform = ({|#3:_Sut|}) => _Sut + _value;
                    return transform(Sut);
                }
            }

            public record Entry(int {|#4:SUT|});
            """;

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0006", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("_sut"));
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0006", DiagnosticSeverity.Warning)
            .WithLocation(1).WithArguments("Sut"));
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0006", DiagnosticSeverity.Warning)
            .WithLocation(2).WithArguments("sut"));
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0006", DiagnosticSeverity.Warning)
            .WithLocation(3).WithArguments("_Sut"));
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0006", DiagnosticSeverity.Warning)
            .WithLocation(4).WithArguments("SUT"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Deconstruction_Pattern_Out_Loop_And_Catch_Declare_Name_Then_Each_Is_Reported()
    {
        _analyzerTester.TestCode = """
            public class Example
            {
                public void Configure(object value)
                {
                    var ({|#0:sut|}, other) = (1, 2);
                    if (value is int {|#1:_sut|}) { }
                    int.TryParse("1", out var {|#2:Sut|});
                    foreach (var {|#3:SUT|} in new[] { 1 }) { }
                    try { }
                    catch (System.Exception {|#4:_Sut|}) { throw; }
                    foreach (var ({|#5:_SUT|}, item) in new[] { (1, 2) }) { }
                }
            }
            """;

        var names = new[] { "sut", "_sut", "Sut", "SUT", "_Sut", "_SUT" };
        for (var location = 0; location < names.Length; location++)
        {
            _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0006", DiagnosticSeverity.Warning)
                .WithLocation(location).WithArguments(names[location]));
        }

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Query_Variables_Are_Forbidden_Then_Each_Declaration_Is_Reported()
    {
        _analyzerTester.TestCode = """
            using System.Linq;

            public class Example
            {
                public void Configure(int[] values)
                {
                    var query = from {|#0:sut|} in values
                                let {|#1:_sut|} = sut + 1
                                join {|#2:Sut|} in values on sut equals Sut into {|#3:_Sut|}
                                select _sut into {|#4:SUT|}
                                select SUT;
                }
            }
            """;

        var names = new[] { "sut", "_sut", "Sut", "_Sut", "SUT" };
        for (var location = 0; location < names.Length; location++)
        {
            _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0006", DiagnosticSeverity.Warning)
                .WithLocation(location).WithArguments(names[location]));
        }

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Identifier_Is_Escaped_Then_Value_Name_Is_Reported()
    {
        _analyzerTester.TestCode = """
            public class Example
            {
                public int Configure(int {|#0:@sut|}) => @sut;
            }
            """;

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0006", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("sut"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Name_Is_Descriptive_Or_Declaration_Is_Outside_Policy_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestCode = """
            public class Sut
            {
                public event System.Action _sut;
                public int SutValue { get; set; }

                public int sut(int validator, int sutValue)
                {
                    _sut?.Invoke();
                    return validator + sutValue;
                }
            }
            """;

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Severity_Is_Not_Configured_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestState.AnalyzerConfigFiles.Clear();
        // Analyzer-testing enables disabled-by-default rules; restore the compiler default for this case.
        _analyzerTester.SolutionTransforms.Add((solution, projectId) =>
        {
            var options = solution.GetProject(projectId)!.CompilationOptions!;
            var diagnostics = options.SpecificDiagnosticOptions.SetItem("DWARD0006", ReportDiagnostic.Default);
            return solution.WithProjectCompilationOptions(projectId, options.WithSpecificDiagnosticOptions(diagnostics));
        });
        _analyzerTester.TestCode = """
            public class Example
            {
                public int Sut { get; set; }
                public int Configure(int sut) => sut;
            }
            """;

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_EditorConfig_Enables_Directory_Then_Only_Its_Top_Level_And_Nested_Files_Are_Reported()
    {
        _analyzerTester.TestState.AnalyzerConfigFiles.Clear();
        _analyzerTester.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", """
            root = true

            [*.cs]
            dotnet_diagnostic.DWARD0006.severity = none

            [Enabled/**.cs]
            dotnet_diagnostic.DWARD0006.severity = warning
            """));
        _analyzerTester.TestState.Sources.Add(("/Enabled/Example.cs", """
            public class EnabledExample
            {
                public int Configure(int {|#0:sut|}) => sut;
            }
            """));
        _analyzerTester.TestState.Sources.Add(("/Disabled.cs", """
            public class DisabledExample
            {
                public int Configure(int sut) => sut;
            }
            """));
        _analyzerTester.TestState.Sources.Add(("/Enabled/Nested/Example.cs", """
            public class NestedExample
            {
                public int Configure(int {|#1:_sut|}) => _sut;
            }
            """));

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0006", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("sut"));
        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0006", DiagnosticSeverity.Warning)
            .WithLocation(1).WithArguments("_sut"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Declaration_Is_Generated_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestState.Sources.Add(("Example.g.cs", """
            // <auto-generated/>
            public class Example
            {
                public int Sut { get; set; }
            }
            """));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Diagnostic_Is_Disabled_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestState.AnalyzerConfigFiles.Clear();
        _analyzerTester.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", """
            is_global = true
            dotnet_diagnostic.DWARD0006.severity = none
            """));
        _analyzerTester.TestCode = """
            public class Example
            {
                public int Configure(int sut) => sut;
            }
            """;

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }
}
