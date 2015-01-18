namespace GitVersionCore.Tests.VersionCalculation.Strategies
{
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

            var baseVersion = sut.GetVersion(contextBuilder.Build());

            baseVersion.ShouldIncrement.ShouldBe(false);
            baseVersion.SemanticVersion.ToString().ShouldBe("1.0.0");
        }

        [Test]
        public void ReturnsNullWhenNoNextVersionIsInConfig()
        {
            var contextBuilder = new GitVersionContextBuilder();
            var sut = new ConfigNextVersionBaseVersionStrategy();

            var baseVersion = sut.GetVersion(contextBuilder.Build());

            baseVersion.ShouldBe(null);
        }
    }
}