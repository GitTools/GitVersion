using GitVersion.Configuration;

namespace GitVersion.Tests;

[TestFixture]
[NonParallelizable]
public class ConfigurationVersionSelectorTests : TestBase
{
    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase("v6", true)]
    [TestCase("V6", true)]
    [TestCase(" v6 ", true)]
    [TestCase("v7", false)]
    [TestCase("V7", false)]
    [TestCase(" v7 ", false)]
    public void ResolvesKnownValues(string? value, bool isV6)
    {
        using var scope = new EnvironmentVariableScope(value);

        ConfigurationVersionSelector.Resolve().ShouldBe(isV6 ? ConfigurationVersion.V6 : ConfigurationVersion.V7);
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
        exception.Message.ShouldContain("v6");
        exception.Message.ShouldContain("v7");
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
