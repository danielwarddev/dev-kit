using System.Collections.Immutable;
using AwesomeAssertions;
using Dward.Analyzers.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace Dward.Analyzers.UnitTests;

public class ConditionalAsyncSuffixCodeFixTests
{
    private readonly CSharpCodeFixTest<ConditionalAsyncSuffixAnalyzer, ConditionalAsyncSuffixCodeFixProvider, DefaultVerifier> _codeFixTester;

    public ConditionalAsyncSuffixCodeFixTests()
    {
        _codeFixTester = new()
        {
            ReferenceAssemblies = new ReferenceAssemblies("net10.0"),
            NumberOfFixAllIterations = 0
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
    [InlineData("Task")]
    [InlineData("Task<int>")]
    [InlineData("ValueTask")]
    [InlineData("ValueTask<int>")]
    [InlineData("void")]
    public async Task When_Private_Async_Method_Is_Eligible_Then_Declaration_And_Method_Group_Are_Renamed(string returnType)
    {
        _codeFixTester.TestCode = $$"""
            using System.Threading.Tasks;

            public class Example
            {
                private async {{returnType}} {|#0:LoadAsync|}()
                {
                    await Task.Yield();
                    throw new System.NotImplementedException();
                }

                public void Configure()
                {
                    var callback = LoadAsync;
                }
            }
            """;
        _codeFixTester.FixedCode = $$"""
            using System.Threading.Tasks;

            public class Example
            {
                private async {{returnType}} Load()
                {
                    await Task.Yield();
                    throw new System.NotImplementedException();
                }

                public void Configure()
                {
                    var callback = Load;
                }
            }
            """;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("Task")]
    [InlineData("Task<int>")]
    [InlineData("ValueTask")]
    [InlineData("ValueTask<int>")]
    [InlineData("void")]
    public async Task When_Public_Async_Method_Is_Eligible_Then_Declaration_And_Caller_Method_Group_Are_Renamed(string returnType)
    {
        _codeFixTester.TestCode = $$"""
            using System.Threading.Tasks;

            public class Example
            {
                public async {{returnType}} {|#0:LoadAsync|}()
                {
                    await Task.Yield();
                    throw new System.NotImplementedException();
                }
            }

            public class Consumer
            {
                public void Configure(Example example)
                {
                    var callback = example.LoadAsync;
                }
            }
            """;
        _codeFixTester.FixedCode = $$"""
            using System.Threading.Tasks;

            public class Example
            {
                public async {{returnType}} Load()
                {
                    await Task.Yield();
                    throw new System.NotImplementedException();
                }
            }

            public class Consumer
            {
                public void Configure(Example example)
                {
                    var callback = example.Load;
                }
            }
            """;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Public_Method_Has_Cross_Project_Callers_Then_References_And_Documentation_Are_Renamed()
    {
        using var workspace = new AdhocWorkspace();
        var libraryProjectId = ProjectId.CreateNewId();
        var consumerProjectId = ProjectId.CreateNewId();
        var libraryDocumentId = DocumentId.CreateNewId(libraryProjectId);
        var consumerDocumentId = DocumentId.CreateNewId(consumerProjectId);
        const string libraryCode = """
            using System.Threading.Tasks;

            public class Library
            {
                public async Task<int> LoadAsync(int value)
                {
                    await Task.Yield();
                    return value;
                }
            }
            """;
        const string consumerCode = """
            using System;
            using System.Threading.Tasks;

            public class Consumer
            {
                /// <summary>Calls <see cref="Library.LoadAsync(int)"/>.</summary>
                public Task<int> Call(Library library) => library.LoadAsync(1);
                public Func<int, Task<int>> Callback(Library library) => library.LoadAsync;
                public string Name => nameof(Library.LoadAsync);
            }
            """;
        var libraryProject = ProjectInfo.Create(libraryProjectId, VersionStamp.Create(), "Library", "Library", LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: new CSharpParseOptions(LanguageVersion.CSharp12),
            metadataReferences: _codeFixTester.TestState.AdditionalReferences);
        var consumerProject = ProjectInfo.Create(consumerProjectId, VersionStamp.Create(), "Consumer", "Consumer", LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: new CSharpParseOptions(LanguageVersion.CSharp12),
            metadataReferences: _codeFixTester.TestState.AdditionalReferences);
        var solution = workspace.CurrentSolution.AddProject(libraryProject).AddProject(consumerProject)
            .AddProjectReference(consumerProjectId, new ProjectReference(libraryProjectId))
            .AddDocument(libraryDocumentId, "Library.cs", SourceText.From(libraryCode))
            .AddDocument(consumerDocumentId, "Consumer.cs", SourceText.From(consumerCode));
        var cancellationToken = TestContext.Current.CancellationToken;
        var document = solution.GetDocument(libraryDocumentId)
            ?? throw new InvalidOperationException("Missing library test document.");
        var compilation = await document.Project.GetCompilationAsync(cancellationToken)
            ?? throw new InvalidOperationException("Missing library test compilation.");
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new ConditionalAsyncSuffixAnalyzer());
        var diagnostics = await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync(cancellationToken);
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostics.Single(), (action, _) => actions.Add(action), cancellationToken);
        var provider = new ConditionalAsyncSuffixCodeFixProvider();

        await provider.RegisterCodeFixesAsync(context);
        actions.Should().ContainSingle();
        var operations = await actions.Single().GetOperationsAsync(cancellationToken);
        var repairedSolution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
        var repairedLibrary = repairedSolution.GetDocument(libraryDocumentId)
            ?? throw new InvalidOperationException("Missing repaired library document.");
        var repairedConsumer = repairedSolution.GetDocument(consumerDocumentId)
            ?? throw new InvalidOperationException("Missing repaired consumer document.");
        var libraryText = await repairedLibrary.GetTextAsync(cancellationToken);
        var consumerText = await repairedConsumer.GetTextAsync(cancellationToken);

        libraryText.ToString().Should().Be("""
            using System.Threading.Tasks;

            public class Library
            {
                public async Task<int> Load(int value)
                {
                    await Task.Yield();
                    return value;
                }
            }
            """);
        consumerText.ToString().Should().Be("""
            using System;
            using System.Threading.Tasks;

            public class Consumer
            {
                /// <summary>Calls <see cref="Library.Load(int)"/>.</summary>
                public Task<int> Call(Library library) => library.Load(1);
                public Func<int, Task<int>> Callback(Library library) => library.Load;
                public string Name => nameof(Library.Load);
            }
            """);
        foreach (var project in repairedSolution.Projects)
        {
            var repairedCompilation = await project.GetCompilationAsync(cancellationToken)
                ?? throw new InvalidOperationException("Missing repaired test compilation.");
            repairedCompilation.GetDiagnostics(cancellationToken).Should().NotContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            var remainingDiagnostics = await repairedCompilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync(cancellationToken);
            remainingDiagnostics.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task When_References_Are_Recursive_Or_Symbolic_Then_Only_Resolved_References_Are_Renamed()
    {
        _codeFixTester.TestCode = """
            using System.Threading.Tasks;

            public class Example
            {
                // Keep LoadAsync in prose.
                /// <summary>Calls <see cref="LoadAsync(int)"/>.</summary>
                private async Task<int> {|#0:LoadAsync|}(int count)
                {
                    await Task.Yield();
                    return count == 0 ? 1 : await LoadAsync(count - 1);
                }

                public Task<int> Call() => LoadAsync(1);
                public string Name => nameof(LoadAsync);
                public Task OtherCall(Other other) => other.LoadAsync();
            }

            public class Other
            {
                public Task LoadAsync() => Task.CompletedTask;
            }
            """;
        _codeFixTester.FixedCode = """
            using System.Threading.Tasks;

            public class Example
            {
                // Keep LoadAsync in prose.
                /// <summary>Calls <see cref="Load(int)"/>.</summary>
                private async Task<int> Load(int count)
                {
                    await Task.Yield();
                    return count == 0 ? 1 : await Load(count - 1);
                }

                public Task<int> Call() => Load(1);
                public string Name => nameof(Load);
                public Task OtherCall(Other other) => other.LoadAsync();
            }

            public class Other
            {
                public Task LoadAsync() => Task.CompletedTask;
            }
            """;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Local_Function_Is_Eligible_Then_Recursion_And_Captured_References_Are_Renamed()
    {
        _codeFixTester.TestCode = """
            using System.Threading.Tasks;

            public class Example
            {
                public async Task<int> Configure(int initial)
                {
                    async Task<int> {|#0:LoadAsync|}(int count)
                    {
                        await Task.Yield();
                        return count == 0 ? initial : await LoadAsync(count - 1);
                    }
                    System.Func<int, Task<int>> callback = LoadAsync;
                    return await callback(1) + await LoadAsync(2);
                }
            }
            """;
        _codeFixTester.FixedCode = """
            using System.Threading.Tasks;

            public class Example
            {
                public async Task<int> Configure(int initial)
                {
                    async Task<int> Load(int count)
                    {
                        await Task.Yield();
                        return count == 0 ? initial : await Load(count - 1);
                    }
                    System.Func<int, Task<int>> callback = Load;
                    return await callback(1) + await Load(2);
                }
            }
            """;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Private_Method_Has_References_In_Another_Partial_File_Then_Both_Files_Are_Updated()
    {
        _codeFixTester.TestState.Sources.Add(("First.cs", """
            using System.Threading.Tasks;

            public partial class Example
            {
                private static async Task<T> {|#0:LoadAsync|}<T>(T value)
                {
                    await Task.Yield();
                    return value;
                }
            }
            """));
        _codeFixTester.TestState.Sources.Add(("Second.cs", """
            using System.Threading.Tasks;

            public partial class Example
            {
                public Task<int> Call() => LoadAsync<int>(1);
            }
            """));
        _codeFixTester.FixedState.Sources.Add(("First.cs", """
            using System.Threading.Tasks;

            public partial class Example
            {
                private static async Task<T> Load<T>(T value)
                {
                    await Task.Yield();
                    return value;
                }
            }
            """));
        _codeFixTester.FixedState.Sources.Add(("Second.cs", """
            using System.Threading.Tasks;

            public partial class Example
            {
                public Task<int> Call() => Load<int>(1);
            }
            """));
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Private_Accessibility_Is_Implicit_And_Identifier_Is_Escaped_Then_References_Are_Renamed()
    {
        _codeFixTester.TestCode = """
            using System.Threading.Tasks;

            public class Example
            {
                async Task {|#0:@LoadAsync|}()
                {
                    await Task.Yield();
                }

                public Task Call() => @LoadAsync();
            }
            """;
        _codeFixTester.FixedCode = """
            using System.Threading.Tasks;

            public class Example
            {
                async Task Load()
                {
                    await Task.Yield();
                }

                public Task Call() => Load();
            }
            """;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("protected")]
    [InlineData("internal")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    public async Task When_Method_Accessibility_Is_Outside_Supported_Scope_Then_No_Fix_Is_Offered(string accessibility)
    {
        var code = $$"""
            using System.Threading.Tasks;

            public class Example
            {
                {{accessibility}} async Task {|#0:LoadAsync|}()
                {
                    await Task.Yield();
                }
            }
            """;
        _codeFixTester.TestCode = code;
        _codeFixTester.FixedCode = code;
        _codeFixTester.NumberOfIncrementalIterations = 0;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Public_Method_Is_Virtual_Then_Override_Contract_Is_Preserved()
    {
        const string code = """
            using System.Threading.Tasks;

            public class Base
            {
                public virtual async Task {|#0:LoadAsync|}()
                {
                    await Task.Yield();
                }
            }

            public class Derived : Base
            {
                public override async Task LoadAsync()
                {
                    await base.LoadAsync();
                }
            }
            """;
        _codeFixTester.TestCode = code;
        _codeFixTester.FixedCode = code;
        _codeFixTester.NumberOfIncrementalIterations = 0;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("private int Load(string value) => 1;")]
    [InlineData("private int Load => 1;")]
    [InlineData("private Task LoadAsync(int value) => Task.CompletedTask;")]
    [InlineData("public void Configure() { System.Action Load = () => { }; System.Func<Task> callback = LoadAsync; }")]
    [InlineData("public Task Call(dynamic example) => example.LoadAsync();")]
    public async Task When_Target_Name_Or_Overload_Set_Is_Conflicted_Then_No_Fix_Is_Offered(string conflictingMember)
    {
        var code = $$"""
            using System.Threading.Tasks;

            public class Example
            {
                {{conflictingMember}}
                private async Task {|#0:LoadAsync|}()
                {
                    await Task.Yield();
                }
            }
            """;
        _codeFixTester.TestCode = code;
        _codeFixTester.FixedCode = code;
        _codeFixTester.NumberOfIncrementalIterations = 0;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("[System.Obsolete]")]
    [InlineData("[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.IUnknown)]")]
    public async Task When_Method_Has_Attributes_Then_No_Fix_Is_Offered(string attributes)
    {
        var code = $$"""
            using System.Threading.Tasks;

            public class Example
            {
                {{attributes}}
                private async Task {|#0:LoadAsync|}()
                {
                    await Task.Yield();
                }
            }
            """;
        _codeFixTester.TestCode = code;
        _codeFixTester.FixedCode = code;
        _codeFixTester.NumberOfIncrementalIterations = 0;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("\"LoadAsync\"")]
    [InlineData("\"loadasync\"")]
    [InlineData("\"Load\" + \"Async\"")]
    [InlineData("$\"LoadAsync:{1}\"")]
    [InlineData("\"Namespace.Example.LoadAsync\"")]
    [InlineData("\"\"\"LoadAsync\"\"\"")]
    [InlineData("nameof(NamePieces.Load) + nameof(NamePieces.Async)")]
    [InlineData("nameof(LoadAsync) + \"\"")]
    public async Task When_Literal_Name_Reference_Is_Detected_Then_No_Fix_Is_Offered(string nameExpression)
    {
        var code = $$"""
            using System.Threading.Tasks;

            public class Example
            {
                public string Name => {{nameExpression}};
                private async Task {|#0:LoadAsync|}()
                {
                    await Task.Yield();
                }
            }

            public class NamePieces
            {
                public void Load() { }
                public void Async() { }
            }
            """;
        _codeFixTester.TestCode = code;
        _codeFixTester.FixedCode = code;
        _codeFixTester.NumberOfIncrementalIterations = 0;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Method_Is_Partial_Then_No_Fix_Is_Offered()
    {
        const string code = """
            using System.Threading.Tasks;

            public partial class Example
            {
                private partial Task LoadAsync();
                private async partial Task {|#0:LoadAsync|}()
                {
                    await Task.Yield();
                }
            }
            """;
        _codeFixTester.TestCode = code;
        _codeFixTester.FixedCode = code;
        _codeFixTester.NumberOfIncrementalIterations = 0;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Parameter_Has_Attribute_Then_No_Fix_Is_Offered()
    {
        const string code = """
            using System.Threading.Tasks;

            public class Example
            {
                private async Task {|#0:LoadAsync|}([System.Runtime.InteropServices.Optional] int value)
                {
                    await Task.Yield();
                }
            }
            """;
        _codeFixTester.TestCode = code;
        _codeFixTester.FixedCode = code;
        _codeFixTester.NumberOfIncrementalIterations = 0;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Local_Function_Has_Attribute_Then_No_Fix_Is_Offered()
    {
        const string code = """
            using System.Threading.Tasks;

            public class Example
            {
                public void Configure()
                {
                    [System.Obsolete]
                    async Task {|#0:LoadAsync|}()
                    {
                        await Task.Yield();
                    }
                }
            }
            """;
        _codeFixTester.TestCode = code;
        _codeFixTester.FixedCode = code;
        _codeFixTester.NumberOfIncrementalIterations = 0;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("int Load = 1; System.Console.WriteLine(Load);")]
    [InlineData("int Load(string text) => text.Length;")]
    [InlineData("{ int Load = 1; System.Console.WriteLine(Load); }")]
    public async Task When_Local_Rename_Would_Conflict_With_Another_Declaration_Then_No_Fix_Is_Offered(string conflictingStatement)
    {
        var code = $$"""
            using System.Threading.Tasks;

            public class Example
            {
                public void Configure()
                {
                    {{conflictingStatement}}
                    async Task {|#0:LoadAsync|}()
                    {
                        await Task.Yield();
                    }
                }
            }
            """;
        _codeFixTester.TestCode = code;
        _codeFixTester.FixedCode = code;
        _codeFixTester.NumberOfIncrementalIterations = 0;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("Async", "")]
    [InlineData("LoadAsyncAsync", "LoadAsync")]
    [InlineData("classAsync", "class")]
    public async Task When_Target_Name_Is_Invalid_Or_Still_Has_Suffix_Then_No_Fix_Is_Offered(string oldName, string newName)
    {
        var code = $$"""
            using System.Threading.Tasks;

            public class Example
            {
                private async Task {|#0:{{oldName}}|}()
                {
                    await Task.Yield();
                }
            }
            """;
        _codeFixTester.TestCode = code;
        _codeFixTester.FixedCode = code;
        _codeFixTester.NumberOfIncrementalIterations = 0;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments(oldName, newName));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Additional_File_Contains_Name_Reference_Then_No_Fix_Is_Offered()
    {
        const string code = """
            using System.Threading.Tasks;

            public class Example
            {
                private async Task {|#0:LoadAsync|}()
                {
                    await Task.Yield();
                }
            }
            """;
        _codeFixTester.TestCode = code;
        _codeFixTester.FixedCode = code;
        _codeFixTester.TestState.AdditionalFiles.Add(("scene.tscn", "method = \"LoadAsync\""));
        _codeFixTester.NumberOfIncrementalIterations = 0;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Reference_Is_In_Generated_File_Then_No_Fix_Is_Offered()
    {
        const string declaration = """
            using System.Threading.Tasks;

            public partial class Example
            {
                private async Task {|#0:LoadAsync|}()
                {
                    await Task.Yield();
                }
            }
            """;
        const string generatedCaller = """
            // <auto-generated/>
            using System.Threading.Tasks;

            public partial class Example
            {
                public Task Call() => LoadAsync();
            }
            """;
        _codeFixTester.TestState.Sources.Add(("First.cs", declaration));
        _codeFixTester.TestState.Sources.Add(("Caller.g.cs", generatedCaller));
        _codeFixTester.FixedState.Sources.Add(("First.cs", declaration));
        _codeFixTester.FixedState.Sources.Add(("Caller.g.cs", generatedCaller));
        _codeFixTester.NumberOfIncrementalIterations = 0;
        _codeFixTester.ExpectedDiagnostics.Add(new DiagnosticResult("DWARD0005", DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments("LoadAsync", "Load"));

        await _codeFixTester.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task When_Declaration_Is_Linked_Across_Projects_Then_No_Fix_Is_Offered()
    {
        using var workspace = new AdhocWorkspace();
        var firstProjectId = ProjectId.CreateNewId();
        var secondProjectId = ProjectId.CreateNewId();
        var firstDocumentId = DocumentId.CreateNewId(firstProjectId);
        var secondDocumentId = DocumentId.CreateNewId(secondProjectId);
        const string code = """
            using System.Threading.Tasks;
            public class Example
            {
                private async Task LoadAsync() { await Task.Yield(); }
            }
            """;
        var path = Path.Combine(AppContext.BaseDirectory, "Linked.cs");
        var firstProject = ProjectInfo.Create(firstProjectId, VersionStamp.Create(), "First", "First", LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: new CSharpParseOptions(LanguageVersion.CSharp12),
            metadataReferences: _codeFixTester.TestState.AdditionalReferences);
        var secondProject = ProjectInfo.Create(secondProjectId, VersionStamp.Create(), "Second", "Second", LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: new CSharpParseOptions(LanguageVersion.CSharp12),
            metadataReferences: _codeFixTester.TestState.AdditionalReferences);
        var solution = workspace.CurrentSolution.AddProject(firstProject).AddProject(secondProject)
            .AddDocument(firstDocumentId, "Linked.cs", SourceText.From(code), filePath: path)
            .AddDocument(secondDocumentId, "Linked.cs", SourceText.From(code), filePath: path);
        var document = solution.GetDocument(firstDocumentId)
            ?? throw new InvalidOperationException("Missing linked test document.");
        var compilation = await document.Project.GetCompilationAsync(TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("Missing linked test compilation.");
        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new ConditionalAsyncSuffixAnalyzer());
        var diagnostics = await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);
        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostics.Single(), (action, _) => actions.Add(action), TestContext.Current.CancellationToken);
        var provider = new ConditionalAsyncSuffixCodeFixProvider();

        await provider.RegisterCodeFixesAsync(context);

        actions.Should().BeEmpty();
    }
}
