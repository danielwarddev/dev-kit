using Dward.Analyzers.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NSubstitute;

namespace Dward.Analyzers.UnitTests;

public class NSubstituteSequentialReturnsCodeFixTests
{
    private readonly CSharpCodeFixTest<NSubstituteSequentialReturnsAnalyzer, NSubstituteSequentialReturnsCodeFixProvider, DefaultVerifier> _codeFixTester;

    public NSubstituteSequentialReturnsCodeFixTests()
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

        var substituteReference = MetadataReference.CreateFromFile(typeof(Substitute).Assembly.Location);
        _codeFixTester.TestState.AdditionalReferences.Add(substituteReference);
        // Formatter-inserted newlines otherwise default to Environment.NewLine, which mismatches the LF test sources on Windows.
        _codeFixTester.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", """
            is_global = true
            end_of_line = lf
            """));
        _codeFixTester.SolutionTransforms.Add((solution, projectId) =>
        {
            var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp12);
            return solution.WithProjectParseOptions(projectId, parseOptions);
        });
    }

    [Theory]
    [InlineData("int[]")]
    [InlineData("int[][]")]
    [InlineData("System.Collections.Generic.List<int>")]
    [InlineData("System.Collections.Generic.IEnumerable<int>")]
    [InlineData("System.Collections.Generic.IReadOnlyList<int>")]
    [InlineData("System.Collections.Generic.HashSet<int>")]
    [InlineData("System.Collections.Immutable.ImmutableArray<int>")]
    public async Task When_Sequential_Empty_Return_Is_Unspecified_Then_Typed_Empty_Value_Is_Added(string returnType)
    {
        _codeFixTester.TestCode = $$"""
            using NSubstitute;

            public class Example
            {
                public void Configure({{returnType}} first)
                {
                    first.Returns(first, {|#0:[]|});
                }
            }
            """;
        _codeFixTester.FixedCode = $$"""
            using NSubstitute;

            public class Example
            {
                public void Configure({{returnType}} first)
                {
                    {{returnType}} emptyReturnValue;
                    first.Returns(first, emptyReturnValue = []);
                }
            }
            """;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0003", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments(returnType));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Collection_Type_Uses_Generic_Elements_Then_Type_Parameters_Are_Preserved()
    {
        _codeFixTester.TestCode = """
            using NSubstitute;
            using System.Collections.Generic;

            public class Example
            {
                public void Configure<T>(IReadOnlyList<T> first)
                {
                    first.Returns(first, {|#0:[]|});
                }
            }
            """;
        _codeFixTester.FixedCode = """
            using NSubstitute;
            using System.Collections.Generic;

            public class Example
            {
                public void Configure<T>(IReadOnlyList<T> first)
                {
                    IReadOnlyList<T> emptyReturnValue;
                    first.Returns(first, emptyReturnValue = []);
                }
            }
            """;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0003", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("System.Collections.Generic.IReadOnlyList<T>"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Collection_Type_Is_Nullable_Then_Local_Preserves_Nullability()
    {
        _codeFixTester.TestCode = """
            #nullable enable
            using NSubstitute;

            public class Example
            {
                public void Configure(int[]? first)
                {
                    first.Returns(first, {|#0:[]|});
                }
            }
            """;
        _codeFixTester.FixedCode = """
            #nullable enable
            using NSubstitute;

            public class Example
            {
                public void Configure(int[]? first)
                {
                    int[]? emptyReturnValue;
                    first.Returns(first, emptyReturnValue = []);
                }
            }
            """;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0003", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("int[]?"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Async_And_Named_Static_Overloads_Are_Repaired_Then_Fix_All_Uses_Distinct_Locals()
    {
        _codeFixTester.TestCode = """
            using NSubstitute;
            using ReturnsApi = NSubstitute.SubstituteExtensions;
            using System.Threading.Tasks;

            public class Example
            {
                public void Configure(int[] first, Task<int[]> task, ValueTask<int[]> valueTask)
                {
                    task.Returns(first, {|#0:[]|});
                    valueTask.Returns(first, {|#1:[]|});
                    ReturnsApi.Returns(first, returnThese: {|#2:[]|}, returnThis: first);
                }
            }
            """;
        _codeFixTester.FixedCode = """
            using NSubstitute;
            using ReturnsApi = NSubstitute.SubstituteExtensions;
            using System.Threading.Tasks;

            public class Example
            {
                public void Configure(int[] first, Task<int[]> task, ValueTask<int[]> valueTask)
                {
                    int[] emptyReturnValue;
                    task.Returns(first, emptyReturnValue = []);
                    int[] emptyReturnValue2;
                    valueTask.Returns(first, emptyReturnValue2 = []);
                    int[] emptyReturnValue3;
                    ReturnsApi.Returns(first, returnThese: emptyReturnValue3 = [], returnThis: first);
                }
            }
            """;
        for (var location = 0; location < 3; location++)
        {
            _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0003", DiagnosticSeverity.Warning)
                .WithLocation(location).WithArguments("int[]"));
        }

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Local_Name_Is_Used_In_Enclosing_And_Nested_Scopes_Then_Collisions_Are_Avoided()
    {
        _codeFixTester.TestCode = """
            using NSubstitute;

            public class Example
            {
                public void Configure(int[] first, int emptyReturnValue)
                {
                    first.Returns(first, {|#0:[]|});
                    {
                        int emptyReturnValue2 = 0;
                        System.Console.WriteLine(emptyReturnValue2);
                    }
                }
            }
            """;
        _codeFixTester.FixedCode = """
            using NSubstitute;

            public class Example
            {
                public void Configure(int[] first, int emptyReturnValue)
                {
                    int[] emptyReturnValue3;
                    first.Returns(first, emptyReturnValue3 = []);
                    {
                        int emptyReturnValue2 = 0;
                        System.Console.WriteLine(emptyReturnValue2);
                    }
                }
            }
            """;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0003", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("int[]"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Multiple_Setups_Share_A_Statement_Then_Fix_All_Preserves_Both_Repairs()
    {
        _codeFixTester.TestCode = """
            using NSubstitute;
            using NSubstitute.Core;

            public class Example
            {
                public void Configure(int[] first)
                {
                    Use(first.Returns(first, {|#0:[]|}), first.Returns(first, {|#1:[]|}));
                }

                public void Use(ConfiguredCall first, ConfiguredCall second) { }
            }
            """;
        _codeFixTester.FixedCode = """
            using NSubstitute;
            using NSubstitute.Core;

            public class Example
            {
                public void Configure(int[] first)
                {
                    int[] emptyReturnValue;
                    int[] emptyReturnValue2;
                    Use(first.Returns(first, emptyReturnValue = []), first.Returns(first, emptyReturnValue2 = []));
                }

                public void Use(ConfiguredCall first, ConfiguredCall second) { }
            }
            """;
        for (var location = 0; location < 2; location++)
        {
            _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0003", DiagnosticSeverity.Warning)
                .WithLocation(location).WithArguments("int[]"));
        }

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Setups_Are_In_Different_Documents_Then_Fix_All_Repairs_Each_Document()
    {
        const string source = """
            using NSubstitute;

            public partial class Example
            {
                public void ConfigureFIRST(int[] first)
                {
                    first.Returns(first, {|#LOCATION:[]|});
                }
            }
            """;
        const string fixedSource = """
            using NSubstitute;

            public partial class Example
            {
                public void ConfigureFIRST(int[] first)
                {
                    int[] emptyReturnValue;
                    first.Returns(first, emptyReturnValue = []);
                }
            }
            """;
        _codeFixTester.TestState.Sources.Add(("First.cs", source.Replace("FIRST", "First").Replace("LOCATION", "0")));
        _codeFixTester.TestState.Sources.Add(("Second.cs", source.Replace("FIRST", "Second").Replace("LOCATION", "1")));
        _codeFixTester.FixedState.Sources.Add(("First.cs", fixedSource.Replace("FIRST", "First")));
        _codeFixTester.FixedState.Sources.Add(("Second.cs", fixedSource.Replace("FIRST", "Second")));
        for (var location = 0; location < 2; location++)
        {
            _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0003", DiagnosticSeverity.Warning)
                .WithLocation(location).WithArguments("int[]"));
        }

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Setup_Is_In_Block_Lambda_Or_Local_Function_Then_Local_Remains_In_That_Scope()
    {
        _codeFixTester.TestCode = """
            using NSubstitute;

            public class Example
            {
                public void Configure(int[] first)
                {
                    System.Action configure = () =>
                    {
                        first.Returns(first, {|#0:[]|});
                    };
                    void ConfigureAgain()
                    {
                        first.Returns(first, {|#1:[]|});
                    }
                    configure();
                    ConfigureAgain();
                }
            }
            """;
        _codeFixTester.FixedCode = """
            using NSubstitute;

            public class Example
            {
                public void Configure(int[] first)
                {
                    System.Action configure = () =>
                    {
                        int[] emptyReturnValue;
                        first.Returns(first, emptyReturnValue = []);
                    };
                    void ConfigureAgain()
                    {
                        int[] emptyReturnValue2;
                        first.Returns(first, emptyReturnValue2 = []);
                    }
                    configure();
                    ConfigureAgain();
                }
            }
            """;
        for (var location = 0; location < 2; location++)
        {
            _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0003", DiagnosticSeverity.Warning)
                .WithLocation(location).WithArguments("int[]"));
        }

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Argument_Contains_Comments_Then_Trivia_And_Evaluation_Positions_Are_Preserved()
    {
        _codeFixTester.TestCode = """
            using NSubstitute;

            public class Example
            {
                public int[] GetValues() => [1];

                public void Configure()
                {
                    // Configure the sequence.
                    GetValues().Returns(GetValues(), /* before */ {|#0:[/* inside */]|} /* after */);
                }
            }
            """;
        _codeFixTester.FixedCode = """
            using NSubstitute;

            public class Example
            {
                public int[] GetValues() => [1];

                public void Configure()
                {
                    // Configure the sequence.
                    int[] emptyReturnValue;
                    GetValues().Returns(GetValues(), /* before */ emptyReturnValue = [/* inside */] /* after */);
                }
            }
            """;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0003", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("int[]"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("public NSubstitute.Core.ConfiguredCall Configure(int[] first) => first.Returns(first, {|#0:[]|});")]
    [InlineData("public void Configure(int[] first) { System.Action configure = () => first.Returns(first, {|#0:[]|}); }")]
    [InlineData("public void Configure(int[] first) { if (first.Length > 0) first.Returns(first, {|#0:[]|}); }")]
    public async Task When_No_Supported_Statement_Scope_Exists_Then_No_Fix_Is_Offered(string member)
    {
        var code = $$"""
            using NSubstitute;

            public class Example
            {
                {{member}}
            }
            """;
        _codeFixTester.TestCode = code;
        _codeFixTester.FixedCode = code;
        _codeFixTester.NumberOfIncrementalIterations = 0;
        _codeFixTester.NumberOfFixAllIterations = 0;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0003", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("int[]"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Return_Type_Cannot_Be_Constructed_From_Empty_Expression_Then_No_Fix_Is_Offered()
    {
        const string code = """
            using NSubstitute;

            public class Example
            {
                public void Configure(System.Collections.IEnumerable first)
                {
                    first.Returns(first, {|#0:[]|});
                }
            }
            """;
        _codeFixTester.TestCode = code;
        _codeFixTester.FixedCode = code;
        _codeFixTester.NumberOfIncrementalIterations = 0;
        _codeFixTester.NumberOfFixAllIterations = 0;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0003", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("System.Collections.IEnumerable"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }
}
