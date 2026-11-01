using System.Diagnostics;
using System.Xml.Linq;
using AwesomeAssertions;

namespace Dward.Godot.Analyzers.UnitTests;

public class PrivateGodotExportCodeFixCliTests
{
    [Fact]
    public async Task When_Format_Repairs_Exports_Then_Names_Resource_Text_And_Outside_Access_Are_Preserved()
    {
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "Dward.Godot.Analyzers", "Dward.Godot.Analyzers.csproj")))
        {
            repository = repository.Parent;
        }
        if (repository is null)
        {
            throw new InvalidOperationException("Cannot locate the Godot analyzer projects.");
        }

        var fixture = Directory.CreateTempSubdirectory("Dward.ExportCodeFix.");
        var cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            var projectPath = Path.Combine(fixture.FullName, "Consumer.csproj");
            var project = new XDocument(new XElement("Project", new XAttribute("Sdk", "Godot.NET.Sdk/4.7.0"),
                new XElement("PropertyGroup", new XElement("TargetFramework", "net10.0")),
                new XElement("ItemGroup",
                    new XElement("ProjectReference",
                        new XAttribute("Include", Path.Combine(repository.FullName, "Dward.Godot.Analyzers", "Dward.Godot.Analyzers.csproj")),
                        new XAttribute("OutputItemType", "Analyzer"), new XAttribute("ReferenceOutputAssembly", "false")),
                    new XElement("ProjectReference",
                        new XAttribute("Include", Path.Combine(repository.FullName, "Dward.Godot.Analyzers.CodeFixes", "Dward.Godot.Analyzers.CodeFixes.csproj")),
                        new XAttribute("OutputItemType", "Analyzer"), new XAttribute("ReferenceOutputAssembly", "false")))));
            project.Save(projectPath);
            var selectedPath = Path.Combine(fixture.FullName, "Selected.cs");
            var excludedPath = Path.Combine(fixture.FullName, "Excluded.cs");
            var outsidePath = Path.Combine(fixture.FullName, "Outside.cs");
            var scenePath = Path.Combine(fixture.FullName, "Selected.tscn");
            var configurationPath = Path.Combine(fixture.FullName, ".editorconfig");
            const string selected = """
                public partial class Selected : Godot.Node
                {
                    // Keep the configuration names.
                    [Godot.Export]
                    public int Count = 3;
                    [Godot.Export]
                    public int Value { get; set; } = 4;
                    public int Read() => Count + Value;
                }
                """;
            const string excluded = """
                public partial class Excluded : Godot.Node
                {
                    [Godot.Export]
                    public int OtherCount = 5;
                }
                """;
            const string outside = """
                public partial class Outside : Godot.Node
                {
                    [Godot.Export]
                    public int SharedCount = 6;
                }
                public class Caller
                {
                    public int Read(Outside node) => node.SharedCount;
                }
                """;
            const string scene = """
                [gd_scene load_steps=2 format=3]
                [ext_resource type="Script" path="res://Selected.cs" id="1"]
                [node name="Selected" type="Node"]
                script = ExtResource("1")
                Count = 9
                Value = 10
                """;
            const string configuration = """
                root = true
                [*.cs]
                dotnet_diagnostic.DWARDGD0001.severity = warning
                """;
            await File.WriteAllTextAsync(selectedPath, selected, cancellationToken);
            await File.WriteAllTextAsync(excludedPath, excluded, cancellationToken);
            await File.WriteAllTextAsync(outsidePath, outside, cancellationToken);
            await File.WriteAllTextAsync(scenePath, scene, cancellationToken);
            await File.WriteAllTextAsync(configurationPath, configuration, cancellationToken);
            var originalProject = await File.ReadAllTextAsync(projectPath, cancellationToken);
            var build = await RunDotnet(fixture.FullName, cancellationToken, "build", projectPath, "--verbosity", "quiet",
                $"-p:RestoreSources={fixture.FullName}", "-p:NuGetAudit=false");
            build.ExitCode.Should().Be(0, build.Output);
            build.Output.Should().Contain("DWARDGD0001").And.NotContain("CS8032").And.NotContain("AD0001");
            var afterBuild = await File.ReadAllTextAsync(selectedPath, cancellationToken);
            afterBuild.Should().Be(selected);

            var repair = await RunDotnet(fixture.FullName, cancellationToken, "format", "analyzers", projectPath,
                "--diagnostics", "DWARDGD0001", "--include", "Selected.cs", "Outside.cs", "--no-restore", "--verbosity", "diagnostic");
            repair.ExitCode.Should().Be(0, repair.Output);
            repair.Output.Should().NotContain("Unable to load").And.NotContain("Exception");
            var changed = await File.ReadAllTextAsync(selectedPath, cancellationToken);
            changed.Should().Be("""
                public partial class Selected : Godot.Node
                {
                    // Keep the configuration names.
                    [Godot.Export]
                    private int Count = 3;
                    [Godot.Export]
                    private int Value { get; set; } = 4;
                    public int Read() => Count + Value;
                }
                """);
            var excludedAfterRepair = await File.ReadAllTextAsync(excludedPath, cancellationToken);
            var outsideAfterRepair = await File.ReadAllTextAsync(outsidePath, cancellationToken);
            excludedAfterRepair.Should().Be(excluded);
            outsideAfterRepair.Should().Be(outside);
            var repeat = await RunDotnet(fixture.FullName, cancellationToken, "format", "analyzers", projectPath,
                "--diagnostics", "DWARDGD0001", "--include", "Selected.cs", "--no-restore", "--verify-no-changes", "--verbosity", "quiet");
            repeat.ExitCode.Should().Be(0, repeat.Output);
            var finalBuild = await RunDotnet(fixture.FullName, cancellationToken, "build", projectPath, "--no-restore", "--verbosity", "quiet");
            finalBuild.ExitCode.Should().Be(0, finalBuild.Output);
            finalBuild.Output.Should().NotContain("CS8032").And.NotContain("AD0001");
            var finalScene = await File.ReadAllTextAsync(scenePath, cancellationToken);
            var finalConfiguration = await File.ReadAllTextAsync(configurationPath, cancellationToken);
            var finalProject = await File.ReadAllTextAsync(projectPath, cancellationToken);
            finalScene.Should().Be(scene);
            finalConfiguration.Should().Be(configuration);
            finalProject.Should().Be(originalProject);
        }
        finally
        {
            fixture.Delete(recursive: true);
        }
    }

    private static async Task<(int ExitCode, string Output)> RunDotnet(string directory, CancellationToken cancellationToken, params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        start.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start dotnet.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var error = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            var standardOutput = await output;
            var standardError = await error;
            return (process.ExitCode, standardOutput + standardError);
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
