namespace GitVersionCore.Tests.VersionCalculation
{
    using GitVersion;
    using GitVersion.VersionCalculation;
    using NUnit.Framework;
    using Shouldly;

    public class NewNextVersionCalculatorTests
    {
        [Test]
        public void ShouldIncrementVersionBasedOnConfig()
        {
            var baseCalculator = new TestBaseVersionCalculator(true, new SemanticVersion(1));
            var sut = new NewNextVersionCalculator(baseCalculator);
            var config = new Config();
            config.Branches.Add("master", new BranchConfig
            {
                Increment = IncrementStrategy.Major
            });
            var context = new GitVersionContextBuilder().WithConfig(config).Build();

            var version = sut.FindVersion(context);

            version.ToString().ShouldBe("2.0.0");
        }

        [Test]
        public void DoesNotIncrementWhenBaseVersionSaysNotTo()
        {
            var baseCalculator = new TestBaseVersionCalculator(false, new SemanticVersion(1));
            var sut = new NewNextVersionCalculator(baseCalculator);
            var config = new Config();
            config.Branches.Add("master", new BranchConfig
            {
                Increment = IncrementStrategy.Major
            });
            var context = new GitVersionContextBuilder().WithConfig(config).Build();

            var version = sut.FindVersion(context);

            version.ToString().ShouldBe("1.0.0");
        }
    }
}