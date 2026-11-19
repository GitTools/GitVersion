using GitVersion.Configuration;
using GitVersion.Git;

namespace GitVersion.App.Tests;

[TestFixture]
[NonParallelizable]
public class FeatureSelectorTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" \t ")]
    [TestCase("v7")]
    [TestCase(" V7 ")]
    public void ParserResolvesKnownValues(string? value)
    {
        using var scope = new SelectorScope(parser: value);

        ArgumentParserVersionSelector.Resolve().ShouldBe(ArgumentParserVersion.V7);
    }

    [TestCase("true")]
    [TestCase("6")]
    [TestCase("v8")]
    public void ParserRejectsUnknownValues(string value)
    {
        using var scope = new SelectorScope(parser: value);

        var exception = Should.Throw<WarningException>(() => ArgumentParserVersionSelector.Resolve());
        exception.Message.ShouldContain(ArgumentParserVersionSelector.EnvironmentVariableName);
        exception.Message.ShouldContain(value);
        exception.Message.ShouldContain("'v7'");
    }

    [TestCase("true")]
    [TestCase("false")]
    [TestCase(" ")]
    public void RetiredBooleanIsRejectedEvenWithNewSelector(string value)
    {
        using var scope = new SelectorScope(parser: "v7", retired: value);

        var exception = Should.Throw<WarningException>(() => ArgumentParserVersionSelector.Resolve());
        exception.Message.ShouldContain(ArgumentParserVersionSelector.RetiredEnvironmentVariableName);
        exception.Message.ShouldContain("GITVERSION_ARGUMENT_PARSER_VERSION=v7");
    }

    [Test]
    [Combinatorial]
    public void CompositionRegistersSelectedImplementations(
        [Values("v7")] string parser,
        [Values("v7")] string configuration,
        [Values("managed")] string backend)
    {
        using var scope = new SelectorScope(parser, configuration, backend);
        using var fixture = new EmptyRepositoryFixture();
        var builder = CliHost.CreateCliHostBuilder([fixture.RepositoryPath]);
        using var host = builder.Build();

        host.Services.GetRequiredService<IArgumentParser>().GetType()
            .ShouldBe(typeof(ArgumentParser));
        host.Services.GetRequiredService<IGitRepository>().GetType().Assembly
            .ShouldBe(typeof(GitVersionManagedGitModule).Assembly);
    }

    [Test]
    public void SelectionLogUsesCapturedValues()
    {
        FeatureSelections selections;
        using (new SelectorScope("v7", "v7", " managed "))
        {
            selections = FeatureSelections.Resolve();
        }

        using var scope = new SelectorScope("v6", "v6", "libgit2");
        var messages = new List<string>();
        selections.Log(new TestLogger<FeatureSelections>(messages.Add));

        messages.ShouldBe(new[] { "Argument parser version: v7; Configuration version: v7; Git backend: managed" });
    }

    [TestCase("v6")]
    [TestCase(" V6 ")]
    public void ParserRejectsRetiredVersion(string value)
    {
        using var scope = new SelectorScope(parser: value);
        var error = Should.Throw<WarningException>(() => ArgumentParserVersionSelector.Resolve());
        error.Message.ShouldContain("removed in GitVersion 7.1");
        error.Message.ShouldContain("POSIX");
        error.Message.ShouldContain("v7");
    }

    private sealed class SelectorScope : IDisposable
    {
        private readonly Dictionary<string, string?> originals = new();

        public SelectorScope(string? parser = null, string? configuration = null, string? backend = null, string? retired = null)
        {
            Set(ArgumentParserVersionSelector.EnvironmentVariableName, parser);
            Set(ConfigurationVersionSelector.EnvironmentVariableName, configuration);
            Set(GitBackendSelector.EnvironmentVariableName, backend);
            Set(ArgumentParserVersionSelector.RetiredEnvironmentVariableName, retired);
        }

        private void Set(string name, string? value)
        {
            this.originals[name] = SysEnv.GetEnvironmentVariable(name);
            SysEnv.SetEnvironmentVariable(name, value);
        }

        public void Dispose()
        {
            foreach (var (name, value) in this.originals)
            {
                SysEnv.SetEnvironmentVariable(name, value);
            }
        }
    }
}
