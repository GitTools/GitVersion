using System.Linq;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using GitVersionCore.Tests.Helpers;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests.VersionCalculation.Strategies
{
    [TestFixture]
    public class ConfigNextVersionBaseVersionStrategyTests : TestBase
    {
        [Test]
        public void ReturnsNullWhenNoNextVersionIsInConfig()
        {
            var baseVersion = GetBaseVersion();

            baseVersion.ShouldBe(null);
        }

        [TestCase("1.0.0", "1.0.0")]
        [TestCase("2", "2.0.0")]
        [TestCase("2.118998723", "2.118998723.0")]
        [TestCase("2.12.654651698", "2.12.654651698")]
        public void ConfigNextVersionTest(string nextVersion, string expectedVersion)
        {
            var baseVersion = GetBaseVersion(new Config
            {
                NextVersion = nextVersion
            });

            baseVersion.ShouldIncrement.ShouldBe(false);
            baseVersion.SemanticVersion.ToString().ShouldBe(expectedVersion);
        }

        private static BaseVersion GetBaseVersion(Config config = null)
        {
            var contextBuilder = new GitVersionContextBuilder();

            if (config != null)
            {
                contextBuilder = contextBuilder.WithConfig(config);
            }

            contextBuilder.Build();
            var strategy = contextBuilder.ServicesProvider.GetServiceForType<IVersionStrategy, ConfigNextVersionVersionStrategy>();

            return strategy.GetVersions().SingleOrDefault();
        }
    }
}
