using System.Diagnostics;
using System.Xml.Linq;

namespace GitVersion.Sonar.Tests;

[TestFixture]
public class ProgramTests
{
    private string directory = null!;

    [SetUp]
    public void SetUp() => this.directory = Directory.CreateTempSubdirectory("sonar-cli-tests-").FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(this.directory, true);

    [Test]
    public async Task UnknownArgumentsReturnNonzeroAndUsage()
    {
        var result = await Invoke("unknown-command");
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Error, Does.Contain("Expected project-ids"));
        Assert.That(result.Output, Is.Empty);
    }

    [Test]
    public async Task ProjectIdsWritesValidOutputUsingExplicitRepositoryPath()
    {
        var repository = Path.Combine(this.directory, "repository");
        var project = Path.Combine(repository, "src/Example/Example.csproj");
        Directory.CreateDirectory(Path.GetDirectoryName(project)!);
        await File.WriteAllTextAsync(project, "<Project/>");
        var destination = Path.Combine(this.directory, "ids.props");
        var result = await Invoke("project-ids", repository, destination);
        Assert.That(result.ExitCode, Is.Zero, result.Error);
        var id = XDocument.Load(destination).Descendants("ProjectGuid").Single();
        Assert.That(id.Value, Is.EqualTo(ProjectIds.Identifier("src/Example/Example.csproj").ToString()));
        Assert.That(id.Attribute("Condition")!.Value, Does.Contain(project));
    }

    [Test]
    public async Task VerifyMissingBundleFailsWithoutCreatingIt()
    {
        var identityPath = Path.Combine(this.directory, "identity.json");
        Handoff.Write(identityPath, new RunIdentity("GitTools/GitVersion", 123, 1, "pull_request", new('a', 40), new('b', 40), new('c', 40), 42, "feature", "main"));
        var missing = Path.Combine(this.directory, "missing-bundle");
        var result = await Invoke("verify", this.directory, missing, identityPath);
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Error, Does.Contain("manifest.json"));
        Assert.That(Directory.Exists(missing), Is.False);
    }

    [TestCase("pull_request")]
    [TestCase("push")]
    public async Task IdentityUsesEventSpecificHeadAnalyzedAndBaseRevisions(string eventName)
    {
        var output = Path.Combine(this.directory, "identity.json");
        var environment = IdentityEnvironment(eventName);
        var result = await InvokeWithEnvironment(environment, "identity", output);
        Assert.That(result.ExitCode, Is.Zero, result.Error);
        var actual = Handoff.Read<RunIdentity>(output);
        var expected = eventName == "pull_request"
            ? new RunIdentity("GitTools/GitVersion", 123, 2, eventName, new('a', 40), new('b', 40), new('c', 40), 42, "feature", "main")
            : new RunIdentity("GitTools/GitVersion", 123, 2, eventName, new('b', 40), new('b', 40), new('b', 40), null, "main", "main");
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task IdentityRejectsMissingRequiredEnvironment()
    {
        var output = Path.Combine(this.directory, "identity.json");
        var environment = IdentityEnvironment("pull_request");
        environment["SONAR_HEAD_SHA"] = null;
        var result = await InvokeWithEnvironment(environment, "identity", output);
        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.Error, Does.Contain("Missing SONAR_HEAD_SHA"));
        Assert.That(File.Exists(output), Is.False);
    }

    private static Dictionary<string, string?> IdentityEnvironment(string eventName) => new()
    {
        ["GITHUB_EVENT_NAME"] = eventName, ["GITHUB_REPOSITORY"] = "GitTools/GitVersion", ["GITHUB_RUN_ID"] = "123",
        ["GITHUB_RUN_ATTEMPT"] = "2", ["GITHUB_SHA"] = new('b', 40), ["GITHUB_REF_NAME"] = "main",
        ["SONAR_HEAD_SHA"] = new('a', 40), ["SONAR_BASE_SHA"] = new('c', 40), ["SONAR_PR"] = "42",
        ["GITHUB_HEAD_REF"] = "feature", ["GITHUB_BASE_REF"] = "main"
    };

    private Task<(int ExitCode, string Output, string Error)> Invoke(params string[] arguments) => InvokeWithEnvironment([], arguments);

    private async Task<(int ExitCode, string Output, string Error)> InvokeWithEnvironment(Dictionary<string, string?> environment, params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = this.directory, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var (name, value) in environment)
        {
            if (value is null) start.Environment.Remove(name);
            else start.Environment[name] = value;
        }
        start.ArgumentList.Add(typeof(SafeFiles).Assembly.Location);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(true); throw; }
        return (process.ExitCode, await stdout, await stderr);
    }
}
