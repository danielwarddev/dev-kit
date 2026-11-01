using System.Diagnostics;
using AwesomeAssertions;

namespace Dward.Godot.Analyzers.UnitTests;

public class GodotFreeProjectBuildTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"GodotBoundary-{Guid.NewGuid():N}");
    private readonly string _projectPath;
    private readonly string _assetsPath;

    public GodotFreeProjectBuildTests()
    {
        Directory.CreateDirectory(_directory);
        _projectPath = Path.Combine(_directory, "Validation.proj");
        _assetsPath = Path.Combine(_directory, "project.assets.json");
        
        var sourceTargets = Path.Combine(AppContext.BaseDirectory, "GodotFreeProject.targets");
        var targetsPath = Path.Combine(_directory, "GodotFreeProject.targets");
        File.Copy(sourceTargets, targetsPath);

        File.WriteAllText(_projectPath, """
            <Project>
              <Import Project="GodotFreeProject.targets" />
              <Target Name="Check">
                <ValidateGodotFreeDependencies AssetsFile="$(MSBuildThisFileDirectory)project.assets.json"
                                               ImportedProjects="$(MSBuildProjectFullPath)"
                                               GodotProjectDirectory="$(GodotProjectDirectory)" />
              </Target>
            </Project>
            """);
    }

    [Theory]
    [InlineData("GodotSharp")]
    [InlineData("GodotSharpEditor")]
    [InlineData("Godot.SourceGenerators")]
    [InlineData("godot.net.sdk")]
    public async Task When_Restored_Graph_Contains_Prohibited_Package_Then_Build_Is_Rejected(string identity)
    {
        var assets = $$"""
            {
              "libraries": {
                "{{identity}}/4.7.0": { "type": "package" }
              }
            }
            """;
        await File.WriteAllTextAsync(_assetsPath, assets, TestContext.Current.CancellationToken);

        var result = await RunDependencyValidation();

        result.ExitCode.Should().NotBe(0);
        result.Output.Should().Contain("DWARDGD0002").And.Contain(identity);
    }

    [Fact]
    public async Task When_Names_Are_Unrelated_Or_Project_Identities_Then_Build_Is_Allowed()
    {
        await File.WriteAllTextAsync(_assetsPath, """
            {
              "libraries": {
                "GodotSharp.Helpers/1.0.0": { "type": "package" },
                "GodotSharp/1.0.0": { "type": "project" }
              }
            }
            """, TestContext.Current.CancellationToken);

        var result = await RunDependencyValidation();

        result.ExitCode.Should().Be(0, result.Output);
    }

    [Fact]
    public async Task When_Assets_Are_Missing_Then_Build_Fails_Explicitly()
    {
        var result = await RunDependencyValidation();

        result.ExitCode.Should().NotBe(0);
        result.Output.Should().Contain("requires a restored assets file");
    }

    [Fact]
    public async Task When_Godot_Sdk_Is_Detected_Then_Build_Is_Rejected()
    {
        await File.WriteAllTextAsync(_assetsPath, """
            { "libraries": {} }
            """, TestContext.Current.CancellationToken);

        var result = await RunDependencyValidation("-p:GodotProjectDirectory=Example");

        result.ExitCode.Should().NotBe(0);
        result.Output.Should().Contain("DWARDGD0002").And.Contain("Godot.NET.Sdk");
    }

    private async Task<(int ExitCode, string Output)> RunDependencyValidation(string? property = null)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add(_projectPath);
        startInfo.ArgumentList.Add("-target:Check");
        startInfo.ArgumentList.Add("-verbosity:quiet");
        startInfo.ArgumentList.Add("-nologo");
        if (property is not null)
        {
            startInfo.ArgumentList.Add(property);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        var cancellationToken = TestContext.Current.CancellationToken;
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
            var output = await outputTask;
            var error = await errorTask;
            return (process.ExitCode, output + error);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }
}
