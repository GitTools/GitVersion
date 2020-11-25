using System;
using GitVersion;
using GitVersion.OutputVariables;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersionExe.Tests
{
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
            argumentParser = sp.GetService<IArgumentParser>();
        }

        [Test]
        public void EmptyOnFetchDisabledBuildServerMeansNoFetchIsTrue()
        {
            var arguments = argumentParser.ParseArguments("");
            arguments.NoFetch.ShouldBe(true);
        }

        private class MockBuildAgent : ICurrentBuildAgent
        {
            public bool CanApplyToCurrentContext()
            {
                throw new NotImplementedException();
            }

            public void WriteIntegration(Action<string> writer, VersionVariables variables, bool updateBuildNumber = true)
            {
                throw new NotImplementedException();
            }

            public string GetCurrentBranch(bool usingDynamicRepos)
            {
                throw new NotImplementedException();
            }

            public bool PreventFetch()
            {
                return true;
            }

            public bool ShouldCleanUpRemotes()
            {
                throw new NotImplementedException();
            }
        }
    }
}
