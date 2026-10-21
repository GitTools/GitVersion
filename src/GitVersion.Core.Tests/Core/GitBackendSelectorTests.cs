using GitVersion.Git;

namespace GitVersion.Tests;

[TestFixture]
[NonParallelizable]
public class GitBackendSelectorTests : TestBase
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t ")]
    [TestCase("managed")]
    [TestCase("Managed")]
    [TestCase(" managed ")]
    public void ResolvesKnownValues(string? value)
    {
        using var scope = new EnvironmentVariableScope(value);

        GitBackendSelector.Resolve().ShouldBe(GitBackend.Managed);
    }

    [TestCase("manged")]
    [TestCase("libgit2sharp")]
    [TestCase("true")]
    public void FailsFastOnUnknownValues(string value)
    {
        using var scope = new EnvironmentVariableScope(value);

        var exception = Should.Throw<WarningException>(() => GitBackendSelector.Resolve());
        exception.Message.ShouldContain(value);
        exception.Message.ShouldContain(GitBackendSelector.EnvironmentVariableName);
        exception.Message.ShouldContain("'managed'");
    }

    [TestCase("libgit2")]
    [TestCase("LIBGIT2")]
    [TestCase(" libgit2 ")]
    public void RemovedBackendHasActionableError(string value)
    {
        using var scope = new EnvironmentVariableScope(value);
        var exception = Should.Throw<WarningException>(() => GitBackendSelector.Resolve());
        exception.Message.ShouldContain("removed in v7.1");
        exception.Message.ShouldContain("managed");
        exception.Message.ShouldContain(GitBackendSelector.EnvironmentVariableName);
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string? original = System.Environment.GetEnvironmentVariable(GitBackendSelector.EnvironmentVariableName);

        public EnvironmentVariableScope(string? value) =>
            System.Environment.SetEnvironmentVariable(GitBackendSelector.EnvironmentVariableName, value);

        public void Dispose() =>
            System.Environment.SetEnvironmentVariable(GitBackendSelector.EnvironmentVariableName, this.original);
    }
}
