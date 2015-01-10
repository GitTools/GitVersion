namespace GitVersionCore.Tests.VersionCalculation
{
    using GitVersion;
    using GitVersion.VersionCalculation;
    using GitVersion.VersionCalculation.BaseVersionCalculators;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    public class BaseVersionCalculatorTests
    {
        [Test]
        public void ChoosesHighestVersionReturnedFromStrategies()
        {
            var context = new GitVersionContextBuilder().Build();
            var sut = new BaseVersionCalculator(new V1Strategy(), new V2Strategy());

            var baseVersion = sut.GetBaseVersion(context);

            baseVersion.SemanticVersion.ToString().ShouldBe("2.0.0");
            baseVersion.ShouldIncrement.ShouldBe(true);
        }

        class V1Strategy : BaseVersionStrategy
        {
            public override BaseVersion GetVersion(GitVersionContext context)
            {
                return new BaseVersion(false, new SemanticVersion(1));
            }
        }

        class V2Strategy : BaseVersionStrategy
        {
            public override BaseVersion GetVersion(GitVersionContext context)
            {
                return new BaseVersion(true, new SemanticVersion(2));
            }
        }
    }
}