using GitVersion.Agents;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using GitVersion.OutputVariables;

namespace GitVersion.App.Tests;

[TestFixture]
public class ArgumentParserOnBuildServerTests : TestBase
{
    private IArgumentParser argumentParser;

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

    private class MockBuildAgent : ICurrentBuildAgent
    {
        public bool IsDefault => false;
        public bool CanApplyToCurrentContext() => throw new NotImplementedException();

        public void WriteIntegration(Action<string> writer, GitVersionVariables variables, bool updateBuildNumber = true) => throw new NotImplementedException();

        public string GetCurrentBranch(bool usingDynamicRepos) => throw new NotImplementedException();

        public bool PreventFetch() => true;

        public bool ShouldCleanUpRemotes() => throw new NotImplementedException();
    }
}
