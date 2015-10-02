namespace GitVersionCore.Tests.VersionCalculation
{
    using System;
    using System.Collections.Generic;
    using GitVersion;
    using GitVersion.VersionCalculation;
    using GitVersion.VersionCalculation.BaseVersionCalculators;
    using LibGit2Sharp;
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
            baseVersion.BaseVersionSource.When().ShouldBe(dateTimeOffset);
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
            baseVersion.BaseVersionSource.When().ShouldBe(when);
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
            baseVersion.BaseVersionSource.When().ShouldBe(when);
        }

        class V1Strategy : BaseVersionStrategy
        {
            readonly Commit when;

            public V1Strategy(DateTimeOffset? when)
            {
                this.when = when == null ? null : new MockCommit { CommitterEx = Constants.Signature(when.Value) };
            }

            public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
            {
                yield return new BaseVersion("Source 1", false, new SemanticVersion(1), when, null);
            }
        }

        class V2Strategy : BaseVersionStrategy
        {
            Commit when;

            public V2Strategy(DateTimeOffset? when)
            {
                this.when = when == null ? null : new MockCommit { CommitterEx = Constants.Signature(when.Value) };
            }

            public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
            {
                yield return new BaseVersion("Source 2", true, new SemanticVersion(2), when, null);
            }
        }
    }
}