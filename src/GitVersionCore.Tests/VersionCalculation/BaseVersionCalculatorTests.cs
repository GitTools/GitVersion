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
using GitVersion.Extensions;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GitVersionCore.Tests.VersionCalculation
{
    [TestFixture]
    public class BaseVersionCalculatorTests : TestBase
    {
        [Test]
        public void ChoosesHighestVersionReturnedFromStrategies()
        {
            var context = new GitVersionContextBuilder().Build();
            var dateTimeOffset = DateTimeOffset.Now;

            var sp = ConfigureServices(services =>
            {
                services.RemoveAll<IVersionStrategy>();
                services.AddSingleton<IVersionStrategy>(new V1Strategy(DateTimeOffset.Now));
                services.AddSingleton<IVersionStrategy>(new V2Strategy(dateTimeOffset));
            });

            var versionCalculator = sp.GetService<IBaseVersionCalculator>();

            var baseVersion = versionCalculator.GetBaseVersion(context);

            baseVersion.SemanticVersion.ToString().ShouldBe("2.0.0");
            baseVersion.ShouldIncrement.ShouldBe(true);
            baseVersion.BaseVersionSource.When().ShouldBe(dateTimeOffset);
        }

        [Test]
        public void UsesWhenFromNextBestMatchIfHighestDoesntHaveWhen()
        {
            var context = new GitVersionContextBuilder().Build();
            var when = DateTimeOffset.Now;

            var sp = ConfigureServices(services =>
            {
                services.RemoveAll<IVersionStrategy>();
                services.AddSingleton<IVersionStrategy>(new V1Strategy(when));
                services.AddSingleton<IVersionStrategy>(new V2Strategy(null));
            });

            var versionCalculator = sp.GetService<IBaseVersionCalculator>();

            var baseVersion = versionCalculator.GetBaseVersion(context);

            baseVersion.SemanticVersion.ToString().ShouldBe("2.0.0");
            baseVersion.ShouldIncrement.ShouldBe(true);
            baseVersion.BaseVersionSource.When().ShouldBe(when);
        }

        [Test]
        public void UsesWhenFromNextBestMatchIfHighestDoesntHaveWhenReversedOrder()
        {
            var context = new GitVersionContextBuilder().Build();
            var when = DateTimeOffset.Now;

            var sp = ConfigureServices(services =>
            {
                services.RemoveAll<IVersionStrategy>();
                services.AddSingleton<IVersionStrategy>(new V1Strategy(null));
                services.AddSingleton<IVersionStrategy>(new V2Strategy(when));
            });

            var versionCalculator = sp.GetService<IBaseVersionCalculator>();

            var baseVersion = versionCalculator.GetBaseVersion(context);

            baseVersion.SemanticVersion.ToString().ShouldBe("2.0.0");
            baseVersion.ShouldIncrement.ShouldBe(true);
            baseVersion.BaseVersionSource.When().ShouldBe(when);
        }

        [Test]
        public void ShouldNotFilterVersion()
        {
            var fakeIgnoreConfig = new TestIgnoreConfig(new ExcludeSourcesContainingExclude());
            var context = new GitVersionContextBuilder().WithConfig(new Config() { Ignore = fakeIgnoreConfig }).Build();
            var version = new BaseVersion(context, "dummy", false, new SemanticVersion(2), new MockCommit(), null);

            var sp = ConfigureServices(services =>
            {
                services.RemoveAll<IVersionStrategy>();
                services.AddSingleton<IVersionStrategy>(new TestVersionStrategy(version));
            });

            var versionCalculator = sp.GetService<IBaseVersionCalculator>();

            var baseVersion = versionCalculator.GetBaseVersion(context);

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

            var sp = ConfigureServices(services =>
            {
                services.RemoveAll<IVersionStrategy>();
                services.AddSingleton<IVersionStrategy>(new TestVersionStrategy(higherVersion, lowerVersion));
            });

            var versionCalculator = sp.GetService<IBaseVersionCalculator>();

            var baseVersion = versionCalculator.GetBaseVersion(context);

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

        private sealed class V1Strategy : IVersionStrategy
        {
            private readonly Commit when;

            public V1Strategy(DateTimeOffset? when)
            {
                this.when = when == null ? null : new MockCommit { CommitterEx = Generate.Signature(when.Value) };
            }

            public IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
            {
                yield return new BaseVersion(context, "Source 1", false, new SemanticVersion(1), when, null);
            }
        }

        private sealed class V2Strategy : IVersionStrategy
        {
            private readonly Commit when;

            public V2Strategy(DateTimeOffset? when)
            {
                this.when = when == null ? null : new MockCommit { CommitterEx = Generate.Signature(when.Value) };
            }

            public IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
            {
                yield return new BaseVersion(context, "Source 2", true, new SemanticVersion(2), when, null);
            }
        }

        private sealed class TestVersionStrategy : IVersionStrategy
        {
            private readonly IEnumerable<BaseVersion> versions;

            public TestVersionStrategy(params BaseVersion[] versions)
            {
                this.versions = versions;
            }

            public IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
            {
                return versions;
            }
        }
    }
}
