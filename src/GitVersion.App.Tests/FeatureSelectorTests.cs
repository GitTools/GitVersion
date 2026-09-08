using GitVersion.Configuration;
using GitVersion.Git;
using GitVersion.Tests;

namespace GitVersion.App.Tests;

[TestFixture]
[NonParallelizable]
public class FeatureSelectorTests
{
    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase(" \t ", false)]
    [TestCase("v6", true)]
    [TestCase(" V6 ", true)]
    [TestCase("v7", false)]
    [TestCase(" V7 ", false)]
    public void ParserResolvesKnownValues(string? value, bool legacy)
    {
        using var scope = new SelectorScope(parser: value);

        ArgumentParserVersionSelector.Resolve().ShouldBe(legacy ? ArgumentParserVersion.V6 : ArgumentParserVersion.V7);
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
        exception.Message.ShouldContain("'v6' and 'v7'");
    }

    [TestCase("true")]
    [TestCase("false")]
    [TestCase(" ")]
    public void RetiredBooleanIsRejectedEvenWithNewSelector(string value)
    {
        using var scope = new SelectorScope(parser: "v7", retired: value);

        var exception = Should.Throw<WarningException>(() => ArgumentParserVersionSelector.Resolve());
        exception.Message.ShouldContain(ArgumentParserVersionSelector.RetiredEnvironmentVariableName);
        exception.Message.ShouldContain("GITVERSION_ARGUMENT_PARSER_VERSION=v6");
    }

    [Test]
    [Combinatorial]
    public void CompositionRegistersSelectedImplementations(
        [Values("v6", "v7")] string parser,
        [Values("v6", "v7")] string configuration,
        [Values("libgit2", "managed")] string backend)
    {
        using var scope = new SelectorScope(parser, configuration, backend);
        using var fixture = new EmptyRepositoryFixture();
        var builder = CliHost.CreateCliHostBuilder([fixture.RepositoryPath]);
        using var host = builder.Build();

        host.Services.GetRequiredService<IArgumentParser>().GetType()
            .ShouldBe(parser == "v6" ? typeof(LegacyArgumentParser) : typeof(ArgumentParser));
        host.Services.GetRequiredService<IGitRepository>().GetType().Assembly
            .ShouldBe(backend == "libgit2" ? typeof(GitVersionLibGit2SharpModule).Assembly : typeof(GitVersionManagedGitModule).Assembly);
    }

    [Test]
    public void SelectionLogUsesCapturedValues()
    {
        FeatureSelections selections;
        using (new SelectorScope("v6", "v6", "libgit2"))
        {
            selections = FeatureSelections.Resolve();
        }

        using var scope = new SelectorScope("v7", "v7", "managed");
        var messages = new List<string>();
        selections.Log(new TestLogger<FeatureSelections>(messages.Add));

        messages.ShouldBe(new[] { "Argument parser version: v6; Configuration version: v6; Git backend: libgit2" });
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
