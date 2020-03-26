using System;
using System.Collections.Generic;
using GitTools.Testing;
using GitVersion;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using GitVersionCore.Tests.Helpers;
using GitVersionCore.Tests.Mocks;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests.VersionCalculation
{
    [TestFixture]
    public class BaseVersionCalculatorTests : TestBase
    {
        [Test]
        public void ChoosesHighestVersionReturnedFromStrategies()
        {
            var dateTimeOffset = DateTimeOffset.Now;
            var versionCalculator = GetBaseVersionCalculator(contextBuilder =>
            {
                contextBuilder.OverrideServices(services =>
                {
                    services.RemoveAll<IVersionStrategy>();
                    services.AddSingleton<IVersionStrategy>(new V1Strategy(DateTimeOffset.Now));
                    services.AddSingleton<IVersionStrategy>(new V2Strategy(dateTimeOffset));
                });
            });

            var baseVersion = versionCalculator.GetBaseVersion();

            baseVersion.SemanticVersion.ToString().ShouldBe("2.0.0");
            baseVersion.ShouldIncrement.ShouldBe(true);
            baseVersion.BaseVersionSource.When().ShouldBe(dateTimeOffset);
        }

        [Test]
        public void UsesWhenFromNextBestMatchIfHighestDoesntHaveWhen()
        {
            var when = DateTimeOffset.Now;

            var versionCalculator = GetBaseVersionCalculator(contextBuilder =>
            {
                contextBuilder.OverrideServices(services =>
                {
                    services.RemoveAll<IVersionStrategy>();
                    services.AddSingleton<IVersionStrategy>(new V1Strategy(when));
                    services.AddSingleton<IVersionStrategy>(new V2Strategy(null));
                });
            });

            var baseVersion = versionCalculator.GetBaseVersion();

            baseVersion.SemanticVersion.ToString().ShouldBe("2.0.0");
            baseVersion.ShouldIncrement.ShouldBe(true);
            baseVersion.BaseVersionSource.When().ShouldBe(when);
        }

        [Test]
        public void UsesWhenFromNextBestMatchIfHighestDoesntHaveWhenReversedOrder()
        {
            var when = DateTimeOffset.Now;

            var versionCalculator = GetBaseVersionCalculator(contextBuilder =>
            {
                contextBuilder.OverrideServices(services =>
                {
                    services.RemoveAll<IVersionStrategy>();
                    services.AddSingleton<IVersionStrategy>(new V1Strategy(null));
                    services.AddSingleton<IVersionStrategy>(new V2Strategy(when));
                });
            });

            var baseVersion = versionCalculator.GetBaseVersion();

            baseVersion.SemanticVersion.ToString().ShouldBe("2.0.0");
            baseVersion.ShouldIncrement.ShouldBe(true);
            baseVersion.BaseVersionSource.When().ShouldBe(when);
        }

        [Test]
        public void ShouldNotFilterVersion()
        {
            var fakeIgnoreConfig = new TestIgnoreConfig(new ExcludeSourcesContainingExclude());
            var version = new BaseVersion("dummy", false, new SemanticVersion(2), new MockCommit(), null);

            var versionCalculator = GetBaseVersionCalculator(contextBuilder =>
            {
                contextBuilder
                    .WithConfig(new Config { Ignore = fakeIgnoreConfig })
                    .OverrideServices(services =>
                    {
                        services.RemoveAll<IVersionStrategy>();
                        services.AddSingleton<IVersionStrategy>(new TestVersionStrategy(version));
                    });
            });

            var baseVersion = versionCalculator.GetBaseVersion();

            baseVersion.Source.ShouldBe(version.Source);
            baseVersion.ShouldIncrement.ShouldBe(version.ShouldIncrement);
            baseVersion.SemanticVersion.ShouldBe(version.SemanticVersion);
        }

        [Test]
        public void ShouldFilterVersion()
        {
            var fakeIgnoreConfig = new TestIgnoreConfig(new ExcludeSourcesContainingExclude());

            var higherVersion = new BaseVersion("exclude", false, new SemanticVersion(2), new MockCommit(), null);
            var lowerVersion = new BaseVersion("dummy", false, new SemanticVersion(1), new MockCommit(), null);

            var versionCalculator = GetBaseVersionCalculator(contextBuilder =>
            {
                contextBuilder
                    .WithConfig(new Config { Ignore = fakeIgnoreConfig })
                    .OverrideServices(services =>
                    {
                        services.RemoveAll<IVersionStrategy>();
                        services.AddSingleton<IVersionStrategy>(new TestVersionStrategy(higherVersion, lowerVersion));
                    });
            });
            var baseVersion = versionCalculator.GetBaseVersion();

            baseVersion.Source.ShouldNotBe(higherVersion.Source);
            baseVersion.SemanticVersion.ShouldNotBe(higherVersion.SemanticVersion);
            baseVersion.Source.ShouldBe(lowerVersion.Source);
            baseVersion.SemanticVersion.ShouldBe(lowerVersion.SemanticVersion);
        }

        private static IBaseVersionCalculator GetBaseVersionCalculator(Action<GitVersionContextBuilder> contextBuilderAction)
        {
            var contextBuilder = new GitVersionContextBuilder();
            contextBuilderAction?.Invoke(contextBuilder);

            contextBuilder.Build();

            return contextBuilder.ServicesProvider.GetService<IBaseVersionCalculator>();
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

            public IEnumerable<BaseVersion> GetVersions()
            {
                yield return new BaseVersion("Source 1", false, new SemanticVersion(1), when, null);
            }
        }

        private sealed class V2Strategy : IVersionStrategy
        {
            private readonly Commit when;

            public V2Strategy(DateTimeOffset? when)
            {
                this.when = when == null ? null : new MockCommit { CommitterEx = Generate.Signature(when.Value) };
            }

            public IEnumerable<BaseVersion> GetVersions()
            {
                yield return new BaseVersion("Source 2", true, new SemanticVersion(2), when, null);
            }
        }

        private sealed class TestVersionStrategy : IVersionStrategy
        {
            private readonly IEnumerable<BaseVersion> versions;

            public TestVersionStrategy(params BaseVersion[] versions)
            {
                this.versions = versions;
            }

            public IEnumerable<BaseVersion> GetVersions()
            {
                return versions;
            }
        }
    }
}
