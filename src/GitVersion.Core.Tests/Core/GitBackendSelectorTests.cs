using GitVersion.Git;

namespace GitVersion.Tests;

[TestFixture]
[NonParallelizable]
public class GitBackendSelectorTests : TestBase
{
    [TestCase(null, true)]
    [TestCase("", true)]
    [TestCase(" \t ", true)]
    [TestCase("libgit2", false)]
    [TestCase("LIBGIT2", false)]
    [TestCase(" LIBGIT2 ", false)]
    [TestCase("managed", true)]
    [TestCase("Managed", true)]
    [TestCase(" managed ", true)]
    public void ResolvesKnownValues(string? value, bool isManaged)
    {
        using var scope = new EnvironmentVariableScope(value);

        GitBackendSelector.Resolve().ShouldBe(isManaged ? GitBackend.Managed : GitBackend.LibGit2);
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
        exception.Message.ShouldContain("'libgit2' and 'managed'");
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
