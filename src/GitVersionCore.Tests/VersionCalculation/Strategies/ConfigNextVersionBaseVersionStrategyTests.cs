using System.Linq;
using GitVersion.Configuration;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.BaseVersionCalculators;
using GitVersionCore.Tests.Helpers;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests.VersionCalculation.Strategies
{
    [TestFixture]
    public class ConfigNextVersionBaseVersionStrategyTests : TestBase
    {
        private IVersionStrategy strategy;
        private GitVersionContextBuilder contextBuilder;

        [SetUp]
        public void SetUp()
        {
            strategy = new ConfigNextVersionVersionStrategy();
            contextBuilder = new GitVersionContextBuilder();
        }

        [Test]
        public void ShouldNotBeIncremented()
        {
            var gitVersionContext = contextBuilder
                .WithConfig(new Config
                {
                    NextVersion = "1.0.0"
                }).Build();

            var baseVersion = strategy.GetVersions(gitVersionContext).Single();

            baseVersion.ShouldIncrement.ShouldBe(false);
            baseVersion.SemanticVersion.ToString().ShouldBe("1.0.0");
        }

        [Test]
        public void ReturnsNullWhenNoNextVersionIsInConfig()
        {
            var gitVersionContext = contextBuilder.Build();

            var baseVersion = strategy.GetVersions(gitVersionContext).SingleOrDefault();

            baseVersion.ShouldBe(null);
        }

        [Test]
        public void NextVersionCanBeInteger()
        {
            var gitVersionContext = contextBuilder
                .WithConfig(new Config
                {
                    NextVersion = "2"
                }).Build();

            var baseVersion = strategy.GetVersions(gitVersionContext).Single();

            baseVersion.SemanticVersion.ToString().ShouldBe("2.0.0");
        }

        [Test]
        public void NextVersionCanHaveEnormousMinorVersion()
        {
            var gitVersionContext = contextBuilder
                .WithConfig(new Config
                {
                    NextVersion = "2.118998723"
                }).Build();

            var baseVersion = strategy.GetVersions(gitVersionContext).Single();

            baseVersion.SemanticVersion.ToString().ShouldBe("2.118998723.0");
        }

        [Test]
        public void NextVersionCanHavePatch()
        {
            var gitVersionContext = contextBuilder
                .WithConfig(new Config
                {
                    NextVersion = "2.12.654651698"
                }).Build();

            var baseVersion = strategy.GetVersions(gitVersionContext).Single();

            baseVersion.SemanticVersion.ToString().ShouldBe("2.12.654651698");
        }
    }
}
