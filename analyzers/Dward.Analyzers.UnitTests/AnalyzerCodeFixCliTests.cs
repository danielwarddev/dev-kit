using System.Diagnostics;
using System.Xml.Linq;
using AwesomeAssertions;

namespace Dward.Analyzers.UnitTests;

public class AnalyzerCodeFixCliTests
{
    [Fact]
    public async Task When_Format_Selects_Async_Rename_Then_Review_Only_Fixer_Does_Not_Run_In_Bulk()
    {
        var repositoryDirectory = FindRepositoryDirectory();
        var fixtureDirectory = Directory.CreateTempSubdirectory("Dward.AsyncRenameCodeFix.");
        var cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            var projectPath = CreateConsumerProject(fixtureDirectory, repositoryDirectory, out var selectedPath);
            const string selectedCode = """
                using System.Threading.Tasks;

                public class Selected
                {
                    public async Task LoadAsync()
                    {
                        await Task.Yield();
                    }

                    public Task Call() => LoadAsync();

                    public async Task Configure()
                    {
                        async Task ParseAsync()
                        {
                            await Task.Yield();
                        }
                        await ParseAsync();
                    }
                }
                """;
            const string configuration = """
                root = true
                [*.cs]
                dotnet_diagnostic.DWARD0005.severity = error
                """;
            var configurationPath = Path.Combine(fixtureDirectory.FullName, ".editorconfig");
            await File.WriteAllTextAsync(selectedPath, selectedCode, cancellationToken);
            await File.WriteAllTextAsync(configurationPath, configuration, cancellationToken);
            var originalProject = await File.ReadAllTextAsync(projectPath, cancellationToken);
            var initialBuild = await RunDotnet(fixtureDirectory.FullName, cancellationToken,
                "build", projectPath, "--verbosity", "quiet",
                $"-p:RestoreSources={fixtureDirectory.FullName}", "-p:NuGetAudit=false");

            initialBuild.ExitCode.Should().NotBe(0, initialBuild.Output);
            initialBuild.Output.Should().Contain("DWARD0005").And.NotContain("CS8032").And.NotContain("AD0001");
            var format = await RunDotnet(fixtureDirectory.FullName, cancellationToken,
                "format", "analyzers", projectPath, "--diagnostics", "DWARD0005",
                "--include", "Selected.cs", "--no-restore", "--verbosity", "diagnostic");

            format.ExitCode.Should().Be(0, format.Output);
            format.Output.Should().NotContain("Unable to load").And.NotContain("Exception");
            var sourceAfterFormat = await File.ReadAllTextAsync(selectedPath, cancellationToken);
            var configurationAfterFormat = await File.ReadAllTextAsync(configurationPath, cancellationToken);
            var projectAfterFormat = await File.ReadAllTextAsync(projectPath, cancellationToken);
            sourceAfterFormat.Should().Be(selectedCode);
            configurationAfterFormat.Should().Be(configuration);
            projectAfterFormat.Should().Be(originalProject);
            var finalBuild = await RunDotnet(fixtureDirectory.FullName, cancellationToken,
                "build", projectPath, "--no-restore", "--verbosity", "quiet");
            finalBuild.ExitCode.Should().NotBe(0, finalBuild.Output);
            finalBuild.Output.Should().Contain("DWARD0005").And.NotContain("CS8032").And.NotContain("AD0001");
        }
        finally
        {
            fixtureDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task When_Cli_Repairs_NSubstitute_Setups_Then_Runtime_Sequences_And_Evaluation_Order_Are_Preserved()
    {
        var repositoryDirectory = FindRepositoryDirectory();
        var fixtureDirectory = Directory.CreateTempSubdirectory("Dward.SequentialReturnCodeFix.");
        var cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            var projectPath = CreateConsumerProject(fixtureDirectory, repositoryDirectory, out var selectedPath);
            var project = XDocument.Load(projectPath);
            var projectElement = project.Root ?? throw new InvalidOperationException("The consuming fixture has no project element.");
            projectElement.Add(
                new XElement("PropertyGroup", new XElement("OutputType", "Exe")),
                new XElement("ItemGroup",
                    new XElement("PackageReference", new XAttribute("Include", "NSubstitute"), new XAttribute("Version", "5.3.0"))));
            project.Save(projectPath);
            const string selectedCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;
                using NSubstitute;

                public interface IValues
                {
                    int[] Next();
                    Task<int[]> NextTask();
                    ValueTask<int[]> NextValueTask();
                    LoggedCollection NextLogged();
                }

                [CollectionBuilder(typeof(LoggedCollectionBuilder), nameof(LoggedCollectionBuilder.Create))]
                public class LoggedCollection : IReadOnlyCollection<int>
                {
                    private readonly int[] _values;
                    public int Count => _values.Length;
                    public LoggedCollection(int[] values) { _values = values; }
                    public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)_values).GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                }

                public static class LoggedCollectionBuilder
                {
                    public static LoggedCollection Create(ReadOnlySpan<int> values)
                    {
                        Program.Events.Add("empty");
                        return new LoggedCollection(values.ToArray());
                    }
                }

                public class Program
                {
                    public static List<string> Events { get; } = [];

                    public static async Task Main()
                    {
                        var values = Substitute.For<IValues>();
                        int[] first = [1];
                        values.Next().Returns(first, []);
                        values.NextTask().Returns(first, []);
                        values.NextValueTask().Returns(first, []);
                        Console.WriteLine("sync:" + VerifySequence(first, values.Next(), values.Next(), values.Next()));
                        Console.WriteLine("task:" + VerifySequence(first, await values.NextTask(), await values.NextTask(), await values.NextTask()));
                        Console.WriteLine("valueTask:" + VerifySequence(first, await values.NextValueTask(), await values.NextValueTask(), await values.NextValueTask()));

                        var staticValues = Substitute.For<IValues>();
                        SubstituteExtensions.Returns(staticValues.Next(), returnThese: [], returnThis: first);
                        Console.WriteLine("static:" + VerifySequence(first, staticValues.Next(), staticValues.Next(), staticValues.Next()));

                        var loggedValues = Substitute.For<IValues>();
                        var firstLogged = new LoggedCollection([1]);
                        Events.Clear();
                        ReadReceiver(loggedValues).Returns(ReadFirst(firstLogged), []);
                        Console.WriteLine("builder:" + string.Join(",", Events) + ";" +
                            VerifySequence(firstLogged, loggedValues.NextLogged(), loggedValues.NextLogged(), loggedValues.NextLogged()));

                        var namedValues = Substitute.For<IValues>();
                        Events.Clear();
                        SubstituteExtensions.Returns(ReadReceiver(namedValues), returnThese: [], returnThis: ReadFirst(firstLogged));
                        Console.WriteLine("namedBuilder:" + string.Join(",", Events) + ";" +
                            VerifySequence(firstLogged, namedValues.NextLogged(), namedValues.NextLogged(), namedValues.NextLogged()));
                    }

                    public static LoggedCollection ReadReceiver(IValues values)
                    {
                        Events.Add("receiver");
                        return values.NextLogged();
                    }

                    public static LoggedCollection ReadFirst(LoggedCollection first)
                    {
                        Events.Add("first");
                        return first;
                    }

                    public static string VerifySequence<T>(T first, T actualFirst, T empty, T repeatedEmpty)
                        where T : IReadOnlyCollection<int>
                    {
                        if (!ReferenceEquals(first, actualFirst) || empty.Count != 0 || !ReferenceEquals(empty, repeatedEmpty))
                        {
                            throw new InvalidOperationException("The return sequence did not include and repeat the intended empty value.");
                        }
                        return $"{first.Count},{empty.Count},{repeatedEmpty.Count}";
                    }
                }
                """;
            const string excludedCode = """
                using NSubstitute;
                public class Excluded
                {
                    public void Configure(int[] first)
                    {
                        first.Returns(first, []);
                    }
                }
                """;
            const string unrelatedCode = "public sealed class Unrelated { }";
            const string configuration = """
                root = true
                [*.cs]
                dotnet_diagnostic.DWARD0003.severity = error
                """;
            var excludedPath = Path.Combine(fixtureDirectory.FullName, "Excluded.cs");
            var unrelatedPath = Path.Combine(fixtureDirectory.FullName, "Unrelated.cs");
            var configurationPath = Path.Combine(fixtureDirectory.FullName, ".editorconfig");
            await File.WriteAllTextAsync(selectedPath, selectedCode, cancellationToken);
            await File.WriteAllTextAsync(excludedPath, excludedCode, cancellationToken);
            await File.WriteAllTextAsync(unrelatedPath, unrelatedCode, cancellationToken);
            await File.WriteAllTextAsync(configurationPath, configuration, cancellationToken);
            var originalProject = await File.ReadAllTextAsync(projectPath, cancellationToken);
            var initialBuild = await RunDotnet(fixtureDirectory.FullName, cancellationToken,
                "build", projectPath, "--verbosity", "quiet",
                $"-p:RestoreSources={fixtureDirectory.FullName}", "-p:NuGetAudit=false");

            initialBuild.ExitCode.Should().NotBe(0, initialBuild.Output);
            initialBuild.Output.Should().Contain("DWARD0003").And.Contain("DWARD0004");
            var sourceAfterBuild = await File.ReadAllTextAsync(selectedPath, cancellationToken);
            sourceAfterBuild.Should().Be(selectedCode);

            var repair = await RunDotnet(fixtureDirectory.FullName, cancellationToken,
                "format", "analyzers", projectPath, "--diagnostics", "DWARD0003",
                "--include", "Selected.cs", "--no-restore", "--verbosity", "diagnostic");

            repair.ExitCode.Should().Be(0, repair.Output);
            repair.Output.Should().NotContain("Unable to load").And.NotContain("Exception");
            var repairedCode = await File.ReadAllTextAsync(selectedPath, cancellationToken);
            var excludedAfterRepair = await File.ReadAllTextAsync(excludedPath, cancellationToken);
            var unrelatedAfterRepair = await File.ReadAllTextAsync(unrelatedPath, cancellationToken);
            repairedCode.Should().Contain("int[] emptyReturnValue;")
                .And.Contain("values.Next().Returns(first, emptyReturnValue = []);")
                .And.NotContain("returnThese: []").And.NotContain(".Returns(first, []);");
            excludedAfterRepair.Should().Be(excludedCode);
            unrelatedAfterRepair.Should().Be(unrelatedCode);

            var verification = await RunDotnet(fixtureDirectory.FullName, cancellationToken,
                "format", "analyzers", projectPath, "--diagnostics", "DWARD0003",
                "--include", "Selected.cs", "--no-restore", "--verify-no-changes", "--verbosity", "quiet");
            verification.ExitCode.Should().Be(0, verification.Output);
            var remainingRepair = await RunDotnet(fixtureDirectory.FullName, cancellationToken,
                "format", "analyzers", projectPath, "--diagnostics", "DWARD0003", "--no-restore", "--verbosity", "quiet");
            remainingRepair.ExitCode.Should().Be(0, remainingRepair.Output);
            var finalBuild = await RunDotnet(fixtureDirectory.FullName, cancellationToken,
                "build", projectPath, "--no-restore", "--verbosity", "quiet");
            finalBuild.ExitCode.Should().Be(0, finalBuild.Output);
            finalBuild.Output.Should().NotContain("DWARD0003").And.Contain("DWARD0004")
                .And.NotContain("CS8032").And.NotContain("AD0001");

            var execution = await RunDotnet(fixtureDirectory.FullName, cancellationToken, "run", "--project", projectPath, "--no-build");
            execution.ExitCode.Should().Be(0, execution.Output);
            var outputLines = execution.Output.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
            outputLines.Should().Equal(
                "sync:1,0,0",
                "task:1,0,0",
                "valueTask:1,0,0",
                "static:1,0,0",
                "builder:receiver,first,empty;1,0,0",
                "namedBuilder:receiver,empty,first;1,0,0");
            var finalConfiguration = await File.ReadAllTextAsync(configurationPath, cancellationToken);
            var finalProject = await File.ReadAllTextAsync(projectPath, cancellationToken);
            finalConfiguration.Should().Be(configuration);
            finalProject.Should().Be(originalProject);
        }
        finally
        {
            fixtureDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task When_Format_Selects_Diagnostic_And_File_Then_Only_Selected_Modifiers_Are_Repaired()
    {
        var repositoryDirectory = FindRepositoryDirectory();

        var fixtureDirectory = Directory.CreateTempSubdirectory("Dward.SealedCodeFix.");
        var cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            var projectPath = CreateConsumerProject(fixtureDirectory, repositoryDirectory, out var selectedPath);
            var excludedPath = Path.Combine(fixtureDirectory.FullName, "Excluded.cs");
            var unrelatedPath = Path.Combine(fixtureDirectory.FullName, "Unrelated.cs");
            const string selectedCode = """
                /// <summary>Retain documentation.</summary>
                public sealed class Selected
                {
                    private sealed record Entry(int Value);
                }
                """;
            const string excludedCode = "public sealed class Excluded { }";
            const string unrelatedCode = """
                public class Unrelated
                {
                    public async System.Threading.Tasks.Task ExecuteAsync()
                    {
                        await System.Threading.Tasks.Task.CompletedTask;
                    }
                }
                """;
            const string configuration = """
                root = true
                [*.cs]
                dotnet_diagnostic.DWARD0004.severity = error
                """;
            var configurationPath = Path.Combine(fixtureDirectory.FullName, ".editorconfig");
            await File.WriteAllTextAsync(selectedPath, selectedCode, cancellationToken);
            await File.WriteAllTextAsync(excludedPath, excludedCode, cancellationToken);
            await File.WriteAllTextAsync(unrelatedPath, unrelatedCode, cancellationToken);
            await File.WriteAllTextAsync(configurationPath, configuration, cancellationToken);
            var originalProject = await File.ReadAllTextAsync(projectPath, cancellationToken);

            var initialBuild = await RunDotnet(fixtureDirectory.FullName, cancellationToken,
                "build", projectPath, "--verbosity", "quiet",
                $"-p:RestoreSources={fixtureDirectory.FullName}", "-p:NuGetAudit=false");

            initialBuild.ExitCode.Should().NotBe(0, initialBuild.Output);
            initialBuild.Output.Should().Contain("DWARD0004").And.Contain("DWARD0005")
                .And.NotContain("CS8032").And.NotContain("AD0001");
            var selectedAfterBuild = await File.ReadAllTextAsync(selectedPath, cancellationToken);
            var excludedAfterBuild = await File.ReadAllTextAsync(excludedPath, cancellationToken);
            selectedAfterBuild.Should().Be(selectedCode);
            excludedAfterBuild.Should().Be(excludedCode);

            var scopedRepair = await RunDotnet(fixtureDirectory.FullName, cancellationToken,
                "format", "analyzers", projectPath, "--diagnostics", "DWARD0004",
                "--include", "Selected.cs", "--no-restore", "--verbosity", "diagnostic");

            scopedRepair.ExitCode.Should().Be(0, scopedRepair.Output);
            scopedRepair.Output.Should().NotContain("Unable to load").And.NotContain("Exception");
            var selectedAfterRepair = await File.ReadAllTextAsync(selectedPath, cancellationToken);
            var excludedAfterRepair = await File.ReadAllTextAsync(excludedPath, cancellationToken);
            var unrelatedAfterRepair = await File.ReadAllTextAsync(unrelatedPath, cancellationToken);
            selectedAfterRepair.Should().Be("""
                /// <summary>Retain documentation.</summary>
                public class Selected
                {
                    private record Entry(int Value);
                }
                """);
            excludedAfterRepair.Should().Be(excludedCode);
            unrelatedAfterRepair.Should().Be(unrelatedCode);

            var scopedVerification = await RunDotnet(fixtureDirectory.FullName, cancellationToken,
                "format", "analyzers", projectPath, "--diagnostics", "DWARD0004",
                "--include", "Selected.cs", "--no-restore", "--verify-no-changes", "--verbosity", "quiet");
            scopedVerification.ExitCode.Should().Be(0, scopedVerification.Output);

            var remainingRepair = await RunDotnet(fixtureDirectory.FullName, cancellationToken,
                "format", "analyzers", projectPath, "--diagnostics", "DWARD0004",
                "--no-restore", "--verbosity", "quiet");
            remainingRepair.ExitCode.Should().Be(0, remainingRepair.Output);
            var finalBuild = await RunDotnet(fixtureDirectory.FullName, cancellationToken,
                "build", projectPath, "--no-restore", "--verbosity", "quiet");

            finalBuild.ExitCode.Should().Be(0, finalBuild.Output);
            finalBuild.Output.Should().NotContain("DWARD0004").And.Contain("DWARD0005")
                .And.NotContain("CS8032").And.NotContain("AD0001");
            var finalExcluded = await File.ReadAllTextAsync(excludedPath, cancellationToken);
            var finalUnrelated = await File.ReadAllTextAsync(unrelatedPath, cancellationToken);
            var finalConfiguration = await File.ReadAllTextAsync(configurationPath, cancellationToken);
            var finalProject = await File.ReadAllTextAsync(projectPath, cancellationToken);
            finalExcluded.Should().Be("public class Excluded { }");
            finalUnrelated.Should().Be(unrelatedCode);
            finalConfiguration.Should().Be(configuration);
            finalProject.Should().Be(originalProject);
        }
        finally
        {
            fixtureDirectory.Delete(recursive: true);
        }
    }

    private static string CreateConsumerProject(DirectoryInfo fixtureDirectory, DirectoryInfo repositoryDirectory,
        out string selectedPath)
    {
        var projectPath = Path.Combine(fixtureDirectory.FullName, "Consumer.csproj");
        var project = new XDocument(
            new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                new XElement("PropertyGroup",
                    new XElement("TargetFramework", "net10.0"),
                    new XElement("EnableNETAnalyzers", "false")),
                new XElement("ItemGroup",
                    new XElement("ProjectReference",
                        new XAttribute("Include", Path.Combine(repositoryDirectory.FullName, "Dward.Analyzers", "Dward.Analyzers.csproj")),
                        new XAttribute("OutputItemType", "Analyzer"),
                        new XAttribute("ReferenceOutputAssembly", "false")),
                    new XElement("ProjectReference",
                        new XAttribute("Include", Path.Combine(repositoryDirectory.FullName, "Dward.Analyzers.CodeFixes", "Dward.Analyzers.CodeFixes.csproj")),
                        new XAttribute("OutputItemType", "Analyzer"),
                        new XAttribute("ReferenceOutputAssembly", "false")))));
        project.Save(projectPath);
        selectedPath = Path.Combine(fixtureDirectory.FullName, "Selected.cs");
        return projectPath;
    }

    private static DirectoryInfo FindRepositoryDirectory()
    {
        var repositoryDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (repositoryDirectory is not null
               && !File.Exists(Path.Combine(repositoryDirectory.FullName, "Dward.Analyzers", "Dward.Analyzers.csproj")))
        {
            repositoryDirectory = repositoryDirectory.Parent;
        }

        if (repositoryDirectory is null)
        {
            throw new InvalidOperationException("Cannot locate the analyzer projects for the CLI consuming fixture.");
        }

        return repositoryDirectory;
    }

    private static async Task<(int ExitCode, string Output)> RunDotnet(
        string workingDirectory,
        CancellationToken cancellationToken,
        params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        startInfo.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start dotnet for the code-fix consuming fixture.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        var standardOutput = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var standardError = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            var output = await standardOutput;
            var errors = await standardError;
            return (process.ExitCode, output + errors);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
            }
        }
    }
}
