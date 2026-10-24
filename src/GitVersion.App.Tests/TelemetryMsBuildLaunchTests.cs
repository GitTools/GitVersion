using System.Diagnostics;
using System.Security;

namespace GitVersion.App.Tests;

/// <summary>Exercises the packaged MSBuild target's actual CLI child-process environment.</summary>
public class TelemetryMsBuildLaunchTests
{
    /// <summary>Marks only the CLI child as MSBuild while preserving opt-outs and inherited environment.</summary>
    [Test]
    public async Task MsBuildTargetScopesCallerToChildAndPreservesOptOut()
    {
        var repository = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (repository != null && !File.Exists(Path.Combine(repository.FullName, "src/GitVersion.MsBuild/msbuild/tools/GitVersion.MsBuild.targets")))
        {
            repository = repository.Parent;
        }
        var repositoryRoot = repository?.FullName ?? throw new InvalidOperationException("Could not locate the GitVersion repository root.");
        var root = Path.Combine(Path.GetTempPath(), "gitversion msbuild telemetry " + Guid.NewGuid());
        Directory.CreateDirectory(root);
        try
        {
            var windows = OperatingSystem.IsWindows();
            var script = Path.Combine(root, windows ? "capture.cmd" : "capture.sh");
            var output = Path.Combine(root, "environment.txt");
            await File.WriteAllTextAsync(script, windows
                ? "@echo off\r\n>\"%TELEMETRY_TEST_OUTPUT%\" echo %GITVERSION_INTERNAL_CALLER%:%DO_NOT_TRACK%:%TELEMETRY_TEST_INHERITED%\r\n"
                : "#!/bin/sh\nprintf '%s:%s:%s' \"$GITVERSION_INTERNAL_CALLER\" \"$DO_NOT_TRACK\" \"$TELEMETRY_TEST_INHERITED\" > \"$TELEMETRY_TEST_OUTPUT\"\n");
            var target = Path.Combine(repositoryRoot, "src/GitVersion.MsBuild/msbuild/tools/GitVersion.MsBuild.targets");
            // Exec already uses cmd.exe on Windows; nesting cmd /c breaks the quoted script and argument paths.
            var command = (windows ? string.Empty : "sh ") + "\"" + script + "\"";
            var project = Path.Combine(root, "launch.proj");
            await File.WriteAllTextAsync(project, $"""
                <Project>
                  <PropertyGroup>
                    <DisableGitVersionTask>false</DisableGitVersionTask>
                    <GitVersionFileExe>{SecurityElement.Escape(command)}</GitVersionFileExe>
                  </PropertyGroup>
                  <Import Project="{SecurityElement.Escape(target)}" />
                </Project>
                """);
            var dotnet = Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "..", "..", "..", windows ? "dotnet.exe" : "dotnet");
            var start = new ProcessStartInfo(dotnet) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            foreach (var argument in new[] { "msbuild", project, "/t:RunGitVersion", "/nologo", "/v:minimal", "/nodeReuse:false" })
            {
                start.ArgumentList.Add(argument);
            }
            start.Environment["GITVERSION_INTERNAL_CALLER"] = "parent-wrapper";
            start.Environment["DO_NOT_TRACK"] = "1";
            start.Environment["TELEMETRY_TEST_INHERITED"] = "retained";
            start.Environment["TELEMETRY_TEST_OUTPUT"] = output;
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                throw;
            }
            process.ExitCode.ShouldBe(0, await stdout + await stderr);
            (await File.ReadAllTextAsync(output)).Trim().ShouldBe("GitVersion.MsBuild:1:retained");
            start.Environment["GITVERSION_INTERNAL_CALLER"].ShouldBe("parent-wrapper");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
