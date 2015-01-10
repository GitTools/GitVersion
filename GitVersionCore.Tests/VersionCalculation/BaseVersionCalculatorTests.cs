namespace GitVersionCore.Tests.VersionCalculation
{
    using System;
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
            var dateTimeOffset = DateTimeOffset.Now;
            var sut = new BaseVersionCalculator(new V1Strategy(DateTimeOffset.Now), new V2Strategy(dateTimeOffset));

            var baseVersion = sut.GetBaseVersion(context);

            baseVersion.SemanticVersion.ToString().ShouldBe("2.0.0");
            baseVersion.ShouldIncrement.ShouldBe(true);
            baseVersion.BaseVersionWhenFrom.ShouldBe(dateTimeOffset);
        }

        [Test]
        public void UsesWhenFromNextBestMatchIfHighestDoesntHaveWhen()
        {
            var context = new GitVersionContextBuilder().Build();
            var when = DateTimeOffset.Now;
            var sut = new BaseVersionCalculator(new V1Strategy(when), new V2Strategy(null));

            var baseVersion = sut.GetBaseVersion(context);

            baseVersion.SemanticVersion.ToString().ShouldBe("2.0.0");
            baseVersion.ShouldIncrement.ShouldBe(true);
            baseVersion.BaseVersionWhenFrom.ShouldBe(when);
        }

        [Test]
        public void UsesWhenFromNextBestMatchIfHighestDoesntHaveWhenReversedOrder()
        {
            var context = new GitVersionContextBuilder().Build();
            var when = DateTimeOffset.Now;
            var sut = new BaseVersionCalculator(new V1Strategy(null), new V2Strategy(when));

            var baseVersion = sut.GetBaseVersion(context);

            baseVersion.SemanticVersion.ToString().ShouldBe("2.0.0");
            baseVersion.ShouldIncrement.ShouldBe(true);
            baseVersion.BaseVersionWhenFrom.ShouldBe(when);
        }

        class V1Strategy : BaseVersionStrategy
        {
            readonly DateTimeOffset? when;

            public V1Strategy(DateTimeOffset? when)
            {
                this.when = when;
            }

            public override BaseVersion GetVersion(GitVersionContext context)
            {
                return new BaseVersion(false, new SemanticVersion(1), when);
            }
        }

        class V2Strategy : BaseVersionStrategy
        {
            DateTimeOffset? when;

            public V2Strategy(DateTimeOffset? when)
            {
                this.when = when;
            }

            public override BaseVersion GetVersion(GitVersionContext context)
            {
                return new BaseVersion(true, new SemanticVersion(2), when);
            }
        }
    }
}