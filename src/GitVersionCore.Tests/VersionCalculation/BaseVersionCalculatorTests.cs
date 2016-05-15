namespace GitVersionCore.Tests.VersionCalculation
{
    using System;
    using System.Collections.Generic;
    using GitTools.Testing;
    using GitVersion;
    using GitVersion.VersionCalculation;
    using GitVersion.VersionCalculation.BaseVersionCalculators;
    using GitVersion.VersionFilters;
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
                this.when = when == null ? null : new MockCommit { CommitterEx = Generate.Signature(when.Value) };
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
                this.when = when == null ? null : new MockCommit { CommitterEx = Generate.Signature(when.Value) };
            }

            public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
            {
                yield return new BaseVersion("Source 2", true, new SemanticVersion(2), when, null);
            }
        }

        [Test]
        public void ShouldNotFilterVersion()
        {
            var fakeIgnoreConfig = new TestIgnoreConfig(new ExcludeSourcesContainingExclude());
            var context = new GitVersionContextBuilder().WithConfig(new Config() { Ignore = fakeIgnoreConfig }).Build();
            var version = new BaseVersion("dummy", false, new SemanticVersion(2), new MockCommit(), null);
            var sut = new BaseVersionCalculator(new TestVersionStrategy(version));

            var baseVersion = sut.GetBaseVersion(context);

            baseVersion.Source.ShouldBe(version.Source);
            baseVersion.ShouldIncrement.ShouldBe(version.ShouldIncrement);
            baseVersion.SemanticVersion.ShouldBe(version.SemanticVersion);
        }

        [Test]
        public void ShouldFilterVersion()
        {
            var fakeIgnoreConfig = new TestIgnoreConfig(new ExcludeSourcesContainingExclude());
            var context = new GitVersionContextBuilder().WithConfig(new Config() { Ignore = fakeIgnoreConfig }).Build();
            var higherVersion = new BaseVersion("exclude", false, new SemanticVersion(2), new MockCommit(), null);
            var lowerVersion = new BaseVersion("dummy", false, new SemanticVersion(1), new MockCommit(), null);
            var sut = new BaseVersionCalculator(new TestVersionStrategy(higherVersion, lowerVersion));

            var baseVersion = sut.GetBaseVersion(context);

            baseVersion.Source.ShouldNotBe(higherVersion.Source);
            baseVersion.SemanticVersion.ShouldNotBe(higherVersion.SemanticVersion);
            baseVersion.Source.ShouldBe(lowerVersion.Source);
            baseVersion.SemanticVersion.ShouldBe(lowerVersion.SemanticVersion);
        }

        internal class TestIgnoreConfig : IgnoreConfig
        {
            private readonly IVersionFilter filter;

            public TestIgnoreConfig(IVersionFilter filter)
            {
                this.filter = filter;
            }

            public override IEnumerable<IVersionFilter> ToFilters()
            {
                yield return filter;
            }
        }

        internal class ExcludeSourcesContainingExclude : IVersionFilter
        {
            public bool Exclude(BaseVersion version, out string reason)
            {
                reason = null;

                if (version.Source.Contains("exclude"))
                {
                    reason = "was excluded";
                    return true;
                }
                return false;
            }
        }

        class TestVersionStrategy : BaseVersionStrategy
        {
            private readonly IEnumerable<BaseVersion> versions;

            public TestVersionStrategy(params BaseVersion[] versions)
            {
                this.versions = versions;
            }

            public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
            {
                return versions;
            }
        }
    }
}