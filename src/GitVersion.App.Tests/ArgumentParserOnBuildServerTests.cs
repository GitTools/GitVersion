using GitVersion.BuildAgents;
using GitVersion.Core.Tests.Helpers;
using GitVersion.OutputVariables;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

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
            services.AddSingleton<IArgumentParser, ArgumentParser>();
            services.AddSingleton<IGlobbingResolver, GlobbingResolver>();
            services.AddSingleton<ICurrentBuildAgent, MockBuildAgent>();
        });
        this.argumentParser = sp.GetService<IArgumentParser>();
    }

    [Test]
    public void EmptyOnFetchDisabledBuildServerMeansNoFetchIsTrue()
    {
        var arguments = this.argumentParser.ParseArguments("");
        arguments.NoFetch.ShouldBe(true);
    }

    private class MockBuildAgent : ICurrentBuildAgent
    {
        public bool CanApplyToCurrentContext() => throw new NotImplementedException();

        public void WriteIntegration(Action<string> writer, VersionVariables variables, bool updateBuildNumber = true) => throw new NotImplementedException();

        public string GetCurrentBranch(bool usingDynamicRepos) => throw new NotImplementedException();

        public bool PreventFetch() => true;

        public bool ShouldCleanUpRemotes() => throw new NotImplementedException();
    }
}
