using GitVersion.Configuration;

namespace GitVersion.Tests;

[TestFixture]
[NonParallelizable]
public class ConfigurationVersionSelectorTests : TestBase
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t ")]
    [TestCase("v7")]
    [TestCase("V7")]
    [TestCase(" v7 ")]
    public void ResolvesKnownValues(string? value)
    {
        using var scope = new EnvironmentVariableScope(value);

        ConfigurationVersionSelector.Resolve().ShouldBe(ConfigurationVersion.V7);
    }

    [TestCase("6")]
    [TestCase("7")]
    [TestCase("true")]
    [TestCase("legacy")]
    public void FailsFastOnUnknownValues(string value)
    {
        using var scope = new EnvironmentVariableScope(value);

        var exception = Should.Throw<WarningException>(() => ConfigurationVersionSelector.Resolve());
        exception.Message.ShouldContain(value);
        exception.Message.ShouldContain("v7");
    }

    [TestCase("v6")]
    [TestCase(" V6 ")]
    public void RejectsRetiredConfigurationWithMigrationGuidance(string value)
    {
        using var scope = new EnvironmentVariableScope(value);
        var error = Should.Throw<WarningException>(() => ConfigurationVersionSelector.Resolve());
        error.Message.ShouldContain("removed in GitVersion 7.1");
        error.Message.ShouldContain("gitversion config migrate");
        error.Message.ShouldContain("v7");
    }

    private sealed class EnvironmentVariableScope : IDisposable
    {
        private readonly string? original = System.Environment.GetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName);

        public EnvironmentVariableScope(string? value) =>
            System.Environment.SetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName, value);

        public void Dispose() =>
            System.Environment.SetEnvironmentVariable(ConfigurationVersionSelector.EnvironmentVariableName, this.original);
    }
}
