namespace GitVersionCore.Tests.VersionCalculation.Strategies
{
    using System.Linq;
    using GitVersion;
    using GitVersion.VersionCalculation.BaseVersionCalculators;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    public class ConfigNextVersionBaseVersionStrategyTests
    {
        [Test]
        public void ShouldNotBeIncremented()
        {
            var contextBuilder = new GitVersionContextBuilder()
                .WithConfig(new Config
                {
                    NextVersion = "1.0.0"
                });
            var sut = new ConfigNextVersionBaseVersionStrategy();

            var baseVersion = sut.GetVersions(contextBuilder.Build()).Single();

            baseVersion.ShouldIncrement.ShouldBe(false);
            baseVersion.SemanticVersion.ToString().ShouldBe("1.0.0");
        }

        [Test]
        public void ReturnsNullWhenNoNextVersionIsInConfig()
        {
            var contextBuilder = new GitVersionContextBuilder();
            var sut = new ConfigNextVersionBaseVersionStrategy();

            var baseVersion = sut.GetVersions(contextBuilder.Build()).SingleOrDefault();

            baseVersion.ShouldBe(null);
        }

        [Test]
        public void NextVersionCanBeInteger()
        {
            var contextBuilder = new GitVersionContextBuilder()
                .WithConfig(new Config
                {
                    NextVersion = "2"
                });
            var sut = new ConfigNextVersionBaseVersionStrategy();

            var baseVersion = sut.GetVersions(contextBuilder.Build()).Single();

            baseVersion.SemanticVersion.ToString().ShouldBe("2.0.0");
        }

        [Test]
        public void NextVersionCanHaveEnormousMinorVersion()
        {
            var contextBuilder = new GitVersionContextBuilder()
                .WithConfig(new Config
                {
                    NextVersion = "2.118998723"
                });
            var sut = new ConfigNextVersionBaseVersionStrategy();

            var baseVersion = sut.GetVersions(contextBuilder.Build()).Single();

            baseVersion.SemanticVersion.ToString().ShouldBe("2.118998723.0");
        }

        [Test]
        public void NextVersionCanHavePatch()
        {
            var contextBuilder = new GitVersionContextBuilder()
                .WithConfig(new Config
                {
                    NextVersion = "2.12.654651698"
                });
            var sut = new ConfigNextVersionBaseVersionStrategy();

            var baseVersion = sut.GetVersions(contextBuilder.Build()).Single();

            baseVersion.SemanticVersion.ToString().ShouldBe("2.12.654651698");
        }
    }
}