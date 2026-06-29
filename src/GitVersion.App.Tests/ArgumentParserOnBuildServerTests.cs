using GitVersion.Agents;
using GitVersion.Extensions;
using GitVersion.OutputVariables;
using GitVersion.Tests;

namespace GitVersion.App.Tests;

[TestFixture]
public class ArgumentParserOnBuildServerTests : TestBase
{
    private IArgumentParser argumentParser = null!;

    [SetUp]
    public void SetUp()
    {
        var sp = ConfigureServices(services =>
        {
            services.AddModule(new GitVersionAppModule());
            services.AddSingleton<ICurrentBuildAgent, MockBuildAgent>();
        });
        this.argumentParser = sp.GetRequiredService<IArgumentParser>();
    }

    [Test]
    public void EmptyOnFetchDisabledBuildServerMeansNoFetchIsTrue()
    {
        var arguments = this.argumentParser.ParseArguments("");
        arguments.NoFetch.ShouldBe(true);
    }

    private sealed class MockBuildAgent : ICurrentBuildAgent
    {
        public bool IsDefault => false;
        public bool CanApplyToCurrentContext() => throw new NotImplementedException();

        public void WriteIntegration(Action<string> writer, GitVersionVariables variables, bool updateBuildNumber = true) => throw new NotImplementedException();

        public string GetCurrentBranch(bool usingDynamicRepos) => throw new NotImplementedException();

        public bool PreventFetch() => true;

        public bool ShouldCleanUpRemotes() => throw new NotImplementedException();
    }
}
