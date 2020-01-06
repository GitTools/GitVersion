using System;
using System.Collections.Generic;
using GitTools.Testing;
using GitVersion;
using GitVersion.Configuration;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.BaseVersionCalculators;
using GitVersion.VersionFilters;
using GitVersionCore.Tests.Mocks;
using LibGit2Sharp;
using NUnit.Framework;
using Shouldly;
using GitVersion.Logging;
using GitVersion.Extensions;

namespace GitVersionCore.Tests.VersionCalculation
{
    [TestFixture]
    public class BaseVersionCalculatorTests : TestBase
    {
        private readonly ILog log;

        public BaseVersionCalculatorTests()
        {
            log = new NullLog();
        }
        [Test]
        public void ChoosesHighestVersionReturnedFromStrategies()
        {
            var context = new GitVersionContextBuilder().Build();
            var dateTimeOffset = DateTimeOffset.Now;

            var versionStrategies = new IVersionStrategy[] { new V1Strategy(DateTimeOffset.Now), new V2Strategy(dateTimeOffset) };
            var sut = new BaseVersionCalculator(log, versionStrategies);

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

            var versionStrategies = new IVersionStrategy[] { new V1Strategy(when), new V2Strategy(null) };
            var sut = new BaseVersionCalculator(log, versionStrategies);

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

            var versionStrategies = new IVersionStrategy[] { new V1Strategy(null), new V2Strategy(when) };
            var sut = new BaseVersionCalculator(log, versionStrategies);

            var baseVersion = sut.GetBaseVersion(context);

            baseVersion.SemanticVersion.ToString().ShouldBe("2.0.0");
            baseVersion.ShouldIncrement.ShouldBe(true);
            baseVersion.BaseVersionSource.When().ShouldBe(when);
        }

        private class V1Strategy : IVersionStrategy
        {
            private readonly Commit when;

            public V1Strategy(DateTimeOffset? when)
            {
                this.when = when == null ? null : new MockCommit { CommitterEx = Generate.Signature(when.Value) };
            }

            public virtual IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
            {
                yield return new BaseVersion(context, "Source 1", false, new SemanticVersion(1), when, null);
            }
        }

        private class V2Strategy : IVersionStrategy
        {
            private readonly Commit when;

            public V2Strategy(DateTimeOffset? when)
            {
                this.when = when == null ? null : new MockCommit { CommitterEx = Generate.Signature(when.Value) };
            }

            public virtual IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
            {
                yield return new BaseVersion(context, "Source 2", true, new SemanticVersion(2), when, null);
            }
        }

        [Test]
        public void ShouldNotFilterVersion()
        {
            var fakeIgnoreConfig = new TestIgnoreConfig(new ExcludeSourcesContainingExclude());
            var context = new GitVersionContextBuilder().WithConfig(new Config() { Ignore = fakeIgnoreConfig }).Build();
            var version = new BaseVersion(context, "dummy", false, new SemanticVersion(2), new MockCommit(), null);

            var versionStrategies = new IVersionStrategy[] { new TestVersionStrategy(version) };
            var sut = new BaseVersionCalculator(log, versionStrategies);

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
            var higherVersion = new BaseVersion(context, "exclude", false, new SemanticVersion(2), new MockCommit(), null);
            var lowerVersion = new BaseVersion(context, "dummy", false, new SemanticVersion(1), new MockCommit(), null);

            var versionStrategies = new IVersionStrategy[] { new TestVersionStrategy(higherVersion, lowerVersion) };
            var sut = new BaseVersionCalculator(log, versionStrategies);

            var baseVersion = sut.GetBaseVersion(context);

            baseVersion.Source.ShouldNotBe(higherVersion.Source);
            baseVersion.SemanticVersion.ShouldNotBe(higherVersion.SemanticVersion);
            baseVersion.Source.ShouldBe(lowerVersion.Source);
            baseVersion.SemanticVersion.ShouldBe(lowerVersion.SemanticVersion);
        }

        private class TestIgnoreConfig : IgnoreConfig
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

        private class ExcludeSourcesContainingExclude : IVersionFilter
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

        private class TestVersionStrategy : IVersionStrategy
        {
            private readonly IEnumerable<BaseVersion> versions;

            public TestVersionStrategy(params BaseVersion[] versions)
            {
                this.versions = versions;
            }

            public virtual IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
            {
                return versions;
            }
        }
    }
}
