using System.Diagnostics;

namespace GitVersion.Sonar.Tests;

[TestFixture]
public class ProcessesTests
{
    [Test]
    public async Task RunDoesNotInheritCredentialEnvironment()
    {
        const string marker = "SONAR_PROCESS_TEST_CHILD";
        const string canary = "sonar-test-only-credential-canary";
        var credentials = new[] { "OP_SERVICE_ACCOUNT_TOKEN", "SONAR_TOKEN", "GH_TOKEN" };
        if (Environment.GetEnvironmentVariable(marker) == "1")
        {
            foreach (var name in credentials)
            {
                Assert.That(Environment.GetEnvironmentVariable(name), Is.EqualTo(canary));
            }

            var output = await Processes.Run("/usr/bin/env", [], Path.GetTempPath());
            foreach (var name in credentials)
            {
                Assert.That(output, Does.Not.Contain(name + "="));
            }

            Assert.That(output, Does.Not.Contain(canary));
            Assert.That(output, Does.Contain("GIT_CONFIG_NOSYSTEM=1"));
            Assert.That(output, Does.Contain("PATH="));
            return;
        }

        // Run the same assertion in an isolated test process; never change the runner's environment.
        var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(typeof(ProcessesTests).Assembly.Location);
        start.ArgumentList.Add("--filter");
        start.ArgumentList.Add("FullyQualifiedName=GitVersion.Sonar.Tests.ProcessesTests.RunDoesNotInheritCredentialEnvironment");
        start.Environment[marker] = "1";
        foreach (var name in credentials)
        {
            start.Environment[name] = canary;
        }

        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(true); throw; }
        var result = await stdout + await stderr;
        Assert.That(process.ExitCode, Is.Zero, result);
        Assert.That(result, Does.Contain("Passed!"));
    }

    [Test]
    public async Task RunRedactsKnownSecretFromSuccessfulToolOutput()
    {
        var output = await Processes.Run("/usr/bin/printf", ["%s", "canary-value"], Path.GetTempPath(), "canary-value");
        Assert.That(output, Is.EqualTo("***"));
    }
}
