using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Dward.Analyzers.UnitTests;

public class ConditionalAsyncSuffixAnalyzerTests
{
    private readonly CSharpAnalyzerTest<ConditionalAsyncSuffixAnalyzer, DefaultVerifier> _analyzerTester;

    public ConditionalAsyncSuffixAnalyzerTests()
    {
        _analyzerTester = new CSharpAnalyzerTest<ConditionalAsyncSuffixAnalyzer, DefaultVerifier>
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
    [InlineData("Task")]
    [InlineData("Task<int>")]
    [InlineData("ValueTask")]
    [InlineData("ValueTask<int>")]
    [InlineData("void")]
    public async Task When_Explicit_Async_Method_Has_No_Counterpart_Then_Name_Is_Reported(string returnType)
    {
        _analyzerTester.TestCode = $$"""
            using System.Threading.Tasks;

            public class Example
            {
                public async {{returnType}} {|#0:LoadAsync|}()
                {
                    await Task.Yield();
                    throw new System.NotImplementedException();
                }
            }
            """;

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("public int Load(string value) => 1;")]
    [InlineData("public string Load(int value) => \"value\";")]
    [InlineData("private int Load(int value) => 1;")]
    [InlineData("public static int Load(int value) => 1;")]
    [InlineData("public int Load<T>(int value) => 1;")]
    [InlineData("public Task<int> Load(int value) => Task.FromResult(1);")]
    [InlineData("public int Load(ref int value) => value;")]
    public async Task When_Same_Named_Overload_Does_Not_Match_Then_Name_Is_Reported(string counterpart)
    {
        _analyzerTester.TestCode = $$"""
            using System.Threading.Tasks;

            public class Example
            {
                {{counterpart}}

                public async Task<int> {|#0:LoadAsync|}(int value)
                {
                    await Task.Yield();
                    return value;
                }
            }
            """;

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Counterparts_Match_Generic_Parameters_And_Optional_Token_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestCode = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;

            public class Example
            {
                public T Load<T>(List<T[]> values) => values[0][0];

                public async Task<U> LoadAsync<U>(List<U[]> values, CancellationToken token)
                {
                    await Task.Yield();
                    return values[0][0];
                }

                public void Execute(CancellationToken token) { }

                public async ValueTask ExecuteAsync(CancellationToken token)
                {
                    await Task.Yield();
                }
            }
            """;

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Extra_Parameter_Is_Not_Trailing_CancellationToken_Then_Name_Is_Reported()
    {
        _analyzerTester.TestCode = """
            using System.Threading;
            using System.Threading.Tasks;

            public class Example
            {
                public int Load(int value) => value;

                public async Task<int> {|#0:LoadAsync|}(CancellationToken token, int value)
                {
                    await Task.Yield();
                    return value;
                }
            }
            """;

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("Task")]
    [InlineData("ValueTask")]
    [InlineData("void")]
    public async Task When_Non_Result_Async_Return_Has_Void_Counterpart_Then_No_Diagnostic_Is_Reported(string returnType)
    {
        _analyzerTester.TestCode = $$"""
            using System.Threading.Tasks;

            public class Example
            {
                public void Execute() { }

                public async {{returnType}} ExecuteAsync()
                {
                    await Task.Yield();
                }
            }
            """;

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Base_Counterpart_Is_Accessible_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestCode = """
            using System.Threading.Tasks;

            public class Base<T>
            {
                public T Load(T value) => value;
            }

            public class Derived : Base<int>
            {
                public async Task<int> LoadAsync(int value)
                {
                    await Task.Yield();
                    return value;
                }
            }
            """;

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Base_Counterpart_Is_Private_Then_Name_Is_Reported()
    {
        _analyzerTester.TestCode = """
            using System.Threading.Tasks;

            public class Base
            {
                private int Load() => 1;
            }

            public class Derived : Base
            {
                private async Task<int> {|#0:LoadAsync|}()
                {
                    await Task.Yield();
                    return 1;
                }
            }
            """;

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Overrides_And_Interface_Implementations_Require_Names_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestCode = """
            using System.Threading.Tasks;

            public interface IContract
            {
                Task LoadAsync();
                Task SaveAsync();
            }

            public class Base
            {
                public virtual Task ExecuteAsync() => Task.CompletedTask;
            }

            public class Example : Base, IContract
            {
                public async Task LoadAsync() { await Task.Yield(); }

                async Task IContract.SaveAsync() { await Task.Yield(); }

                public override async Task ExecuteAsync() { await Task.Yield(); }
            }
            """;

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Default_Interface_Method_Is_Explicitly_Async_Then_Name_Is_Reported()
    {
        _analyzerTester.TestCode = """
            using System.Threading.Tasks;

            public interface IContract
            {
                async Task {|#0:LoadAsync|}()
                {
                    await Task.Yield();
                }
            }
            """;

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Method_Is_Not_Explicitly_Async_Or_Has_No_Suffix_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestCode = """
            using System.Threading.Tasks;

            public class Example
            {
                public Task LoadAsync() => Task.CompletedTask;
                public int SaveAsync() => 1;

                public async Task Execute() { await Task.Yield(); }
            }
            """;

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Extension_Method_Has_Matching_Counterpart_Then_Only_Unmatched_Name_Is_Reported()
    {
        _analyzerTester.TestCode = """
            using System.Threading.Tasks;

            public static class Extensions
            {
                public static int Load(this string value) => value.Length;

                public static async Task<int> LoadAsync(this string value)
                {
                    await Task.Yield();
                    return value.Length;
                }

                public static async Task {|#0:SaveAsync|}(this string value)
                {
                    await Task.Yield();
                }
            }
            """;

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("SaveAsync", "Save"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Local_Counterpart_Is_Not_In_Same_Block_Then_Only_Unmatched_Local_Is_Reported()
    {
        _analyzerTester.TestCode = """
            using System.Threading.Tasks;

            public class Example
            {
                public void Configure()
                {
                    int Load(int value) => value;
                    int Save(int value) => value;
                    async Task<int> LoadAsync(int value)
                    {
                        await Task.Yield();
                        return value;
                    }

                    {
                        async Task<int> {|#0:SaveAsync|}(int value)
                        {
                            await Task.Yield();
                            return value;
                        }
                    }
                }
            }
            """;

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("SaveAsync", "Save"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Async_Streams_Have_Synchronous_Equivalents_Then_Only_Unmatched_Name_Is_Reported()
    {
        _analyzerTester.TestCode = """
            using System.Collections.Generic;
            using System.Threading.Tasks;

            public class Example
            {
                public IEnumerable<int> Load() { yield return 1; }

                public async IAsyncEnumerable<int> LoadAsync()
                {
                    await Task.Yield();
                    yield return 1;
                }

                public IEnumerator<int> Enumerate() => Load().GetEnumerator();

                public async IAsyncEnumerator<int> EnumerateAsync()
                {
                    await Task.Yield();
                    yield return 1;
                }

                public async IAsyncEnumerable<int> {|#0:SaveAsync|}()
                {
                    await Task.Yield();
                    yield return 1;
                }
            }
            """;

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("SaveAsync", "Save"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Custom_Async_Return_Type_Is_Used_Then_Non_Task_Counterpart_Is_Required()
    {
        _analyzerTester.TestCode = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;

            [AsyncMethodBuilder(typeof(CustomBuilder))]
            public class CustomTask
            {
                public TaskAwaiter GetAwaiter() => Task.CompletedTask.GetAwaiter();
            }

            public struct CustomBuilder
            {
                public static CustomBuilder Create() => default;
                public CustomTask Task => new CustomTask();
                public void SetResult() { }
                public void SetException(Exception error) { }
                public void SetStateMachine(IAsyncStateMachine machine) { }
                public void Start<T>(ref T machine) where T : IAsyncStateMachine { }
                public void AwaitOnCompleted<T, U>(ref T awaiter, ref U machine)
                    where T : INotifyCompletion where U : IAsyncStateMachine { }
                public void AwaitUnsafeOnCompleted<T, U>(ref T awaiter, ref U machine)
                    where T : ICriticalNotifyCompletion where U : IAsyncStateMachine { }
            }

            public class Example
            {
                public int Load() => 1;

                public async CustomTask LoadAsync() { await Task.Yield(); }

                public CustomTask Save() => new CustomTask();

                public async CustomTask {|#0:SaveAsync|}() { await Task.Yield(); }
            }
            """;

        _analyzerTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("SaveAsync", "Save"));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Counterpart_Is_In_Other_Partial_Declaration_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestState.Sources.Add(("First.cs", """
            using TaskAlias = System.Threading.Tasks.Task<int>;

            public partial class Example
            {
                public async TaskAlias LoadAsync()
                {
                    await System.Threading.Tasks.Task.Yield();
                    return 1;
                }
            }
            """));
        _analyzerTester.TestState.Sources.Add(("Second.cs", """
            public partial class Example
            {
                public int Load() => 1;
            }
            """));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Declaration_Is_Generated_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestState.Sources.Add(("Example.g.cs", """
            // <auto-generated/>
            using System.Threading.Tasks;

            public class Example
            {
                public async Task LoadAsync() { await Task.Yield(); }
            }
            """));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Rule_Is_Disabled_Then_No_Diagnostic_Is_Reported()
    {
        _analyzerTester.TestCode = """
            using System.Threading.Tasks;

            public class Example
            {
                public async Task LoadAsync() { await Task.Yield(); }
            }
            """;
        _analyzerTester.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", """
            is_global = true
            dotnet_diagnostic.DWARD0005.severity = none
            """));

        await _analyzerTester.RunAsync(TestContext.Current.CancellationToken);
    }
}
