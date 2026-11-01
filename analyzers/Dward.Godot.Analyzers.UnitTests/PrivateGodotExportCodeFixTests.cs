using Dward.Godot.Analyzers.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Dward.Godot.Analyzers.UnitTests;

public class PrivateGodotExportCodeFixTests
{
    private readonly CSharpCodeFixTest<PrivateGodotExportAnalyzer, PrivateGodotExportCodeFixProvider, DefaultVerifier> _tester;

    public PrivateGodotExportCodeFixTests()
    {
        _tester = new() { ReferenceAssemblies = new ReferenceAssemblies("net10.0") };
        var references = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "ReferenceAssemblies"), "*.dll");
        if (references.Length == 0)
        {
            throw new InvalidOperationException("Missing test reference assemblies.");
        }
        foreach (var path in references)
        {
            _tester.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(path));
        }
        _tester.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(global::Godot.ExportAttribute).Assembly.Location));
        _tester.SolutionTransforms.Add((solution, projectId) =>
            solution.WithProjectParseOptions(projectId, new CSharpParseOptions(LanguageVersion.CSharp12)));
    }

    [Theory]
    [InlineData("public")]
    [InlineData("protected")]
    [InlineData("internal")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    public async Task When_No_Outside_Access_Exists_Then_Field_And_Property_Become_Private(string accessibility)
    {
        _tester.TestCode = $$"""
            public class Example
            {
                [Godot.Export]
                {{accessibility}} int {|#0:Count|} = 3;
                [Godot.Export]
                {{accessibility}} int {|#1:Value|} { get; set; } = 4;
                public int Read() => Count + Value;
            }
            """;
        _tester.FixedCode = """
            public class Example
            {
                [Godot.Export]
                private int Count = 3;
                [Godot.Export]
                private int Value { get; set; } = 4;
                public int Read() => Count + Value;
            }
            """;
        _tester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0001", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Count"));
        _tester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0001", DiagnosticSeverity.Warning).WithLocation(1).WithArguments("Value"));
        await _tester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Modifiers_Have_Comments_Then_Triva_Attributes_And_Names_Are_Preserved()
    {
        _tester.TestCode = """
            using ExportAlias = Godot.ExportAttribute;
            public class Example
            {
                /// <summary>Keep this configuration.</summary>
                [ExportAlias]
                protected /* keep */ internal int {|#0:@Count|} = 3;
                public int Read() => Count;
            }
            """;
        _tester.FixedCode = """
            using ExportAlias = Godot.ExportAttribute;
            public class Example
            {
                /// <summary>Keep this configuration.</summary>
                [ExportAlias]
                private /* keep */ int @Count = 3;
                public int Read() => Count;
            }
            """;
        _tester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0001", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Count"));
        await _tester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("public int Read(Example example) => example.Count;")]
    [InlineData("public void Write(Example example) { example.Count = 4; }")]
    [InlineData("public string Name => nameof(Example.Count);")]
    [InlineData("public int Read(dynamic example) => example.Count;")]
    public async Task When_Outside_Access_Needs_Changing_Then_No_Fix_Is_Offered(string caller)
    {
        var code = $$"""
            public class Example
            {
                [Godot.Export]
                public int {|#0:Count|};
            }
            public class Caller
            {
                {{caller}}
            }
            """;
        _tester.TestCode = code;
        _tester.FixedCode = code;
        _tester.NumberOfIncrementalIterations = 0;
        _tester.NumberOfFixAllIterations = 0;
        _tester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0001", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Count"));
        await _tester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("public int Count { get; private set; }")]
    [InlineData("public required int Count { get; set; }")]
    [InlineData("public virtual int Count { get; set; }")]
    public async Task When_Property_Cannot_Preserve_Declaration_Behavior_Then_No_Fix_Is_Offered(string member)
    {
        var code = $$"""
            public class Example
            {
                [Godot.Export]
                {{member.Replace("Count", "{|#0:Count|}")}}
            }
            """;
        _tester.TestCode = code;
        _tester.FixedCode = code;
        _tester.NumberOfIncrementalIterations = 0;
        _tester.NumberOfFixAllIterations = 0;
        _tester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0001", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Count"));
        await _tester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Field_Declaration_Has_Multiple_Variables_Then_No_Coordinated_Modifier_Edit_Is_Offered()
    {
        const string code = """
            public class Example
            {
                [Godot.Export]
                public int {|#0:First|}, {|#1:Second|};
            }
            """;
        _tester.TestCode = code;
        _tester.FixedCode = code;
        _tester.NumberOfIncrementalIterations = 0;
        _tester.NumberOfFixAllIterations = 0;
        _tester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0001", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("First"));
        _tester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0001", DiagnosticSeverity.Warning).WithLocation(1).WithArguments("Second"));
        await _tester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Derived_Class_Accesses_Protected_Export_Then_No_Fix_Is_Offered()
    {
        const string code = """
            public class Example
            {
                [Godot.Export]
                protected int {|#0:Count|};
            }
            public class Derived : Example
            {
                public int Read() => Count;
            }
            """;
        _tester.TestCode = code;
        _tester.FixedCode = code;
        _tester.NumberOfIncrementalIterations = 0;
        _tester.NumberOfFixAllIterations = 0;
        _tester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0001", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Count"));
        await _tester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Private_Export_Would_Rebind_Derived_Access_Then_No_Fix_Is_Offered()
    {
        const string code = """
            public class Base
            {
                public int Count = 1;
            }
            public class Example : Base
            {
                [Godot.Export]
                public new int {|#0:Count|} = 2;
            }
            public class Derived : Example
            {
                public int Read() => Count;
            }
            """;
        _tester.TestCode = code;
        _tester.FixedCode = code;
        _tester.NumberOfIncrementalIterations = 0;
        _tester.NumberOfFixAllIterations = 0;
        _tester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0001", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Count"));
        await _tester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Partial_Type_Uses_Exports_In_Another_File_Then_Fix_All_Composes_Without_Renaming()
    {
        _tester.TestState.Sources.Add(("First.cs", """
            public partial class Example
            {
                [Godot.Export]
                public int {|#0:Count|} = 3;
            }
            """));
        _tester.TestState.Sources.Add(("Second.cs", """
            public partial class Example
            {
                [Godot.Export]
                public int {|#1:Value|} { get; set; } = 4;
                public int Read() => Count + Value;
            }
            """));
        _tester.FixedState.Sources.Add(("First.cs", """
            public partial class Example
            {
                [Godot.Export]
                private int Count = 3;
            }
            """));
        _tester.FixedState.Sources.Add(("Second.cs", """
            public partial class Example
            {
                [Godot.Export]
                private int Value { get; set; } = 4;
                public int Read() => Count + Value;
            }
            """));
        _tester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0001", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Count"));
        _tester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0001", DiagnosticSeverity.Warning).WithLocation(1).WithArguments("Value"));
        await _tester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Another_Project_Reads_Export_Then_No_Fix_Is_Offered()
    {
        const string code = """
            public class Example
            {
                [Godot.Export]
                public int {|#0:Count|} = 3;
            }
            """;
        _tester.TestCode = code;
        _tester.FixedCode = code;
        var consumer = new ProjectState("Consumer", LanguageNames.CSharp, "Test", "cs")
        {
            ReferenceAssemblies = _tester.ReferenceAssemblies
        };
        foreach (var reference in _tester.TestState.AdditionalReferences)
        {
            consumer.AdditionalReferences.Add(reference);
        }
        consumer.AdditionalProjectReferences.Add(_tester.TestState.Name);
        consumer.Sources.Add(("Consumer.cs", """
            public class Caller
            {
                public int Read(Example example) => example.Count;
            }
            """));
        _tester.TestState.AdditionalProjects.Add("Consumer", consumer);
        _tester.NumberOfIncrementalIterations = 0;
        _tester.NumberOfFixAllIterations = 0;
        _tester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARDGD0001", DiagnosticSeverity.Warning).WithLocation(0).WithArguments("Count"));
        await _tester.RunAsync(TestContext.Current.CancellationToken);
    }
}
