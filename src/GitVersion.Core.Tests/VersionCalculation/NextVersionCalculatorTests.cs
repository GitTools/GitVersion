using GitTools.Testing;
using GitVersion.Common;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Core.Tests.IntegrationTests;
using GitVersion.Logging;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Shouldly;

namespace GitVersion.Core.Tests.VersionCalculation;

public class NextVersionCalculatorTests : TestBase
{
    [Test]
    public void ShouldIncrementVersionBasedOnConfig()
    {
        var contextBuilder = new GitVersionContextBuilder();

        contextBuilder.Build();

        contextBuilder.ServicesProvider.ShouldNotBeNull();
        var nextVersionCalculator = contextBuilder.ServicesProvider.GetRequiredService<INextVersionCalculator>();
        nextVersionCalculator.ShouldNotBeNull();

        var nextVersion = nextVersionCalculator.FindVersion();

        nextVersion.IncrementedVersion.ToString().ShouldBe("0.0.1");
    }

    [Test]
    public void DoesNotIncrementWhenBaseVersionSaysNotTo()
    {
        var contextBuilder = new GitVersionContextBuilder();

        contextBuilder.WithConfig(new Model.Configuration.GitVersionConfiguration() { NextVersion = "1.0.0" }).Build();

        contextBuilder.ServicesProvider.ShouldNotBeNull();
        var nextVersionCalculator = contextBuilder.ServicesProvider.GetRequiredService<INextVersionCalculator>();

        nextVersionCalculator.ShouldNotBeNull();

        var nextVersion = nextVersionCalculator.FindVersion();

        nextVersion.IncrementedVersion.ToString().ShouldBe("1.0.0");
    }

    [Test]
    public void AppliesBranchPreReleaseTag()
    {
        var contextBuilder = new GitVersionContextBuilder();

        contextBuilder.WithDevelopBranch().Build();

        contextBuilder.ServicesProvider.ShouldNotBeNull();
        var nextVersionCalculator = contextBuilder.ServicesProvider.GetRequiredService<INextVersionCalculator>();
        nextVersionCalculator.ShouldNotBeNull();

        var nextVersion = nextVersionCalculator.FindVersion();

        nextVersion.IncrementedVersion.ToString("f").ShouldBe("0.1.0-alpha.1+0");
    }

    [Test]
    public void PreReleaseTagCanUseBranchName()
    {
        var configuration = new Model.Configuration.GitVersionConfiguration
        {
            NextVersion = "1.0.0",
            Branches = new Dictionary<string, BranchConfiguration>
            {
                {
                    "custom", new BranchConfiguration
                    {
                        Regex = "custom/",
                        Tag = "useBranchName",
                        SourceBranches = new HashSet<string>()
                    }
                }
            }
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("custom/foo");
        fixture.MakeACommit();

        fixture.AssertFullSemver("1.0.0-foo.1+3", configuration);
    }

    [Test]
    public void PreReleaseVersionMainline()
    {
        var configuration = new Model.Configuration.GitVersionConfiguration
        {
            VersioningMode = VersioningMode.Mainline,
            NextVersion = "1.0.0"
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo("foo");
        fixture.MakeACommit();

        fixture.AssertFullSemver("1.0.0-foo.1", configuration);
    }

    [Test]
    public void MergeIntoMainline()
    {
        var configuration = new Model.Configuration.GitVersionConfiguration
        {
            VersioningMode = VersioningMode.Mainline,
            NextVersion = "1.0.0"
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo("foo");
        fixture.MakeACommit();
        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("foo");

        fixture.AssertFullSemver("1.0.0", configuration);
    }

    [Test]
    public void MergeFeatureIntoMainline()
    {
        var configuration = new Model.Configuration.GitVersionConfiguration
        {
            VersioningMode = VersioningMode.Mainline
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.ApplyTag("1.0.0");
        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.0.1-foo.1", configuration);
        fixture.ApplyTag("1.0.1-foo.1");

        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo");
        fixture.AssertFullSemver("1.0.1", configuration);
    }

    [Test]
    public void MergeFeatureIntoMainlineWithMinorIncrement()
    {
        var configuration = new Model.Configuration.GitVersionConfiguration
        {
            VersioningMode = VersioningMode.Mainline,
            Branches = new Dictionary<string, BranchConfiguration>
            {
                { "feature", new BranchConfiguration { Increment = IncrementStrategy.Minor } }
            },
            Ignore = new IgnoreConfig { ShAs = new List<string>() },
            MergeMessageFormats = new Dictionary<string, string>()
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.ApplyTag("1.0.0");
        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-foo.1", configuration);
        fixture.ApplyTag("1.1.0-foo.1");

        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo");
        fixture.AssertFullSemver("1.1.0", configuration);
    }

    [Test]
    public void MergeFeatureIntoMainlineWithMinorIncrementAndThenMergeHotfix()
    {
        var configuration = new Model.Configuration.GitVersionConfiguration
        {
            VersioningMode = VersioningMode.Mainline,
            Branches = new Dictionary<string, BranchConfiguration>
            {
                { "feature", new BranchConfiguration { Increment = IncrementStrategy.Minor } }
            },
            Ignore = new IgnoreConfig { ShAs = new List<string>() },
            MergeMessageFormats = new Dictionary<string, string>()
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.ApplyTag("1.0.0");
        fixture.AssertFullSemver("1.0.0", configuration);

        fixture.BranchTo("feature/foo");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.0-foo.1", configuration);
        fixture.ApplyTag("1.1.0-foo.1");

        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("feature/foo");
        fixture.AssertFullSemver("1.1.0", configuration);
        fixture.ApplyTag("1.1.0");

        fixture.BranchTo("hotfix/bar");
        fixture.MakeACommit();
        fixture.AssertFullSemver("1.1.1-beta.1", configuration);
        fixture.ApplyTag("1.1.1-beta.1");

        fixture.Checkout(MainBranch);
        fixture.MergeNoFF("hotfix/bar");
        fixture.AssertFullSemver("1.1.1", configuration);
    }

    [Test]
    public void PreReleaseTagCanUseBranchNameVariable()
    {
        var configuration = new Model.Configuration.GitVersionConfiguration
        {
            NextVersion = "1.0.0",
            Branches = new Dictionary<string, BranchConfiguration>
            {
                {
                    "custom", new BranchConfiguration
                    {
                        Regex = "custom/",
                        Tag = "alpha.{BranchName}",
                        SourceBranches = new HashSet<string>()
                    }
                }
            }
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit();
        fixture.BranchTo("develop");
        fixture.MakeACommit();
        fixture.BranchTo("custom/foo");
        fixture.MakeACommit();

        fixture.AssertFullSemver("1.0.0-alpha.foo.1+3", configuration);
    }

    [Test]
    public void PreReleaseNumberShouldBeScopeToPreReleaseLabelInContinuousDelivery()
    {
        var configuration = new Model.Configuration.GitVersionConfiguration
        {
            VersioningMode = VersioningMode.ContinuousDelivery,
            Branches = new Dictionary<string, BranchConfiguration>
            {
                {
                    MainBranch, new BranchConfiguration
                    {
                        Tag = "beta"
                    }
                }
            }
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.Repository.MakeACommit();

        fixture.Repository.CreateBranch("feature/test");
        Commands.Checkout(fixture.Repository, "feature/test");
        fixture.Repository.MakeATaggedCommit("0.1.0-test.1");
        fixture.Repository.MakeACommit();

        fixture.AssertFullSemver("0.1.0-test.2+1", configuration);

        Commands.Checkout(fixture.Repository, MainBranch);
        fixture.Repository.Merge("feature/test", Generate.SignatureNow());

        fixture.AssertFullSemver("0.1.0-beta.1+1", configuration); // just one commit no fast forward merge here.
    }

    [Test]
    public void GetNextVersionOnNonMainlineBranchWithoutCommitsShouldWorkNormally()
    {
        var configuration = new Model.Configuration.GitVersionConfiguration
        {
            VersioningMode = VersioningMode.Mainline,
            NextVersion = "1.0.0"
        };

        using var fixture = new EmptyRepositoryFixture();
        fixture.MakeACommit("initial commit");
        fixture.BranchTo("feature/f1");
        fixture.AssertFullSemver("1.0.0-f1.0", configuration);
    }

    [Test]
    public void ChoosesHighestVersionReturnedFromStrategies()
    {
        // Arrange
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = TestConfigurationBuilder.New.Build();
        var context = new GitVersionContext(branchMock, null, configuration, null, 0);
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        var effectiveConfiguration = context.GetEffectiveConfiguration(branchMock);
        var effectiveBranchConfiguration = new EffectiveBranchConfiguration(branchMock, effectiveConfiguration);
        var effectiveBranchConfigurationFinderMock = Substitute.For<IEffectiveBranchConfigurationFinder>();
        effectiveBranchConfigurationFinderMock.GetConfigurations(branchMock, configuration).Returns(new[] { effectiveBranchConfiguration });
        var incrementStrategyFinderMock = Substitute.For<IIncrementStrategyFinder>();
        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(Enumerable.Empty<IBranch>());
        var dateTimeOffset = DateTimeOffset.Now;
        var versionStrategies = new IVersionStrategy[] { new V1Strategy(DateTimeOffset.Now), new V2Strategy(dateTimeOffset) };
        var unitUnderTest = new NextVersionCalculator(Substitute.For<ILog>(), Substitute.For<IMainlineVersionCalculator>(),
            repositoryStoreMock, new(context), versionStrategies, effectiveBranchConfigurationFinderMock, incrementStrategyFinderMock);

        // Act
        var nextVersion = unitUnderTest.FindVersion();

        // Assert
        nextVersion.BaseVersion.SemanticVersion.ToString().ShouldBe("2.0.0");
        nextVersion.BaseVersion.ShouldIncrement.ShouldBe(true);
        nextVersion.BaseVersion.BaseVersionSource.ShouldNotBeNull();
        nextVersion.BaseVersion.BaseVersionSource.When.ShouldBe(dateTimeOffset);
    }

    [Test]
    public void UsesWhenFromNextBestMatchIfHighestDoesntHaveWhen()
    {
        // Arrange
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = TestConfigurationBuilder.New.Build();
        var context = new GitVersionContext(branchMock, null, configuration, null, 0);
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        var effectiveConfiguration = context.GetEffectiveConfiguration(branchMock);
        var effectiveBranchConfiguration = new EffectiveBranchConfiguration(branchMock, effectiveConfiguration);
        var effectiveBranchConfigurationFinderMock = Substitute.For<IEffectiveBranchConfigurationFinder>();
        effectiveBranchConfigurationFinderMock.GetConfigurations(branchMock, configuration).Returns(new[] { effectiveBranchConfiguration });
        var incrementStrategyFinderMock = Substitute.For<IIncrementStrategyFinder>();
        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(Enumerable.Empty<IBranch>());
        var when = DateTimeOffset.Now;
        var versionStrategies = new IVersionStrategy[] { new V1Strategy(when), new V2Strategy(null) };
        var unitUnderTest = new NextVersionCalculator(Substitute.For<ILog>(), Substitute.For<IMainlineVersionCalculator>(),
            repositoryStoreMock, new(context), versionStrategies, effectiveBranchConfigurationFinderMock, incrementStrategyFinderMock);

        // Act
        var nextVersion = unitUnderTest.FindVersion();

        // Assert
        nextVersion.BaseVersion.SemanticVersion.ToString().ShouldBe("2.0.0");
        nextVersion.BaseVersion.ShouldIncrement.ShouldBe(true);
        nextVersion.BaseVersion.BaseVersionSource.ShouldNotBeNull();
        nextVersion.BaseVersion.BaseVersionSource.When.ShouldBe(when);
    }

    [Test]
    public void UsesWhenFromNextBestMatchIfHighestDoesntHaveWhenReversedOrder()
    {
        // Arrange
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());
        var configuration = TestConfigurationBuilder.New.Build();
        var context = new GitVersionContext(branchMock, null, configuration, null, 0);
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        var effectiveConfiguration = context.GetEffectiveConfiguration(branchMock);
        var effectiveBranchConfiguration = new EffectiveBranchConfiguration(branchMock, effectiveConfiguration);
        var effectiveBranchConfigurationFinderMock = Substitute.For<IEffectiveBranchConfigurationFinder>();
        effectiveBranchConfigurationFinderMock.GetConfigurations(branchMock, configuration).Returns(new[] { effectiveBranchConfiguration });
        var incrementStrategyFinderMock = Substitute.For<IIncrementStrategyFinder>();
        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(Enumerable.Empty<IBranch>());
        var when = DateTimeOffset.Now;
        var versionStrategies = new IVersionStrategy[] { new V2Strategy(null), new V1Strategy(when) };
        var unitUnderTest = new NextVersionCalculator(Substitute.For<ILog>(), Substitute.For<IMainlineVersionCalculator>(),
            repositoryStoreMock, new(context), versionStrategies, effectiveBranchConfigurationFinderMock, incrementStrategyFinderMock);

        // Act
        var nextVersion = unitUnderTest.FindVersion();

        // Assert
        nextVersion.BaseVersion.SemanticVersion.ToString().ShouldBe("2.0.0");
        nextVersion.BaseVersion.ShouldIncrement.ShouldBe(true);
        nextVersion.BaseVersion.BaseVersionSource.ShouldNotBeNull();
        nextVersion.BaseVersion.BaseVersionSource.When.ShouldBe(when);
    }

    [Test]
    public void ShouldNotFilterVersion()
    {
        // Arrange
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());
        var fakeIgnoreConfig = new TestIgnoreConfig(new ExcludeSourcesContainingExclude());
        var configuration = TestConfigurationBuilder.New.WithIgnoreConfig(fakeIgnoreConfig).Build();
        var context = new GitVersionContext(branchMock, null, configuration, null, 0);
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        var effectiveConfiguration = context.GetEffectiveConfiguration(branchMock);
        var effectiveBranchConfiguration = new EffectiveBranchConfiguration(branchMock, effectiveConfiguration);
        var effectiveBranchConfigurationFinderMock = Substitute.For<IEffectiveBranchConfigurationFinder>();
        effectiveBranchConfigurationFinderMock.GetConfigurations(branchMock, configuration).Returns(new[] { effectiveBranchConfiguration });
        var incrementStrategyFinderMock = Substitute.For<IIncrementStrategyFinder>();
        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(Enumerable.Empty<IBranch>());
        var version = new BaseVersion("dummy", false, new SemanticVersion(2), GitToolsTestingExtensions.CreateMockCommit(), null);
        var versionStrategies = new IVersionStrategy[] { new TestVersionStrategy(version) };
        var unitUnderTest = new NextVersionCalculator(Substitute.For<ILog>(), Substitute.For<IMainlineVersionCalculator>(),
            repositoryStoreMock, new(context), versionStrategies, effectiveBranchConfigurationFinderMock, incrementStrategyFinderMock);

        // Act
        var nextVersion = unitUnderTest.FindVersion();

        // Assert
        nextVersion.BaseVersion.Source.ShouldBe(version.Source);
        nextVersion.BaseVersion.ShouldIncrement.ShouldBe(version.ShouldIncrement);
        nextVersion.BaseVersion.SemanticVersion.ShouldBe(version.SemanticVersion);
    }

    [Test]
    public void ShouldFilterVersion()
    {
        // Arrange
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());
        var fakeIgnoreConfig = new TestIgnoreConfig(new ExcludeSourcesContainingExclude());
        var configuration = TestConfigurationBuilder.New.WithIgnoreConfig(fakeIgnoreConfig).Build();
        var context = new GitVersionContext(branchMock, null, configuration, null, 0);
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        var effectiveConfiguration = context.GetEffectiveConfiguration(branchMock);
        var effectiveBranchConfiguration = new EffectiveBranchConfiguration(branchMock, effectiveConfiguration);
        var effectiveBranchConfigurationFinderMock = Substitute.For<IEffectiveBranchConfigurationFinder>();
        effectiveBranchConfigurationFinderMock.GetConfigurations(branchMock, configuration).Returns(new[] { effectiveBranchConfiguration });
        var incrementStrategyFinderMock = Substitute.For<IIncrementStrategyFinder>();
        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(Enumerable.Empty<IBranch>());
        var higherVersion = new BaseVersion("exclude", false, new SemanticVersion(2), GitToolsTestingExtensions.CreateMockCommit(), null);
        var lowerVersion = new BaseVersion("dummy", false, new SemanticVersion(1), GitToolsTestingExtensions.CreateMockCommit(), null);
        var versionStrategies = new IVersionStrategy[] { new TestVersionStrategy(higherVersion, lowerVersion) };
        var unitUnderTest = new NextVersionCalculator(Substitute.For<ILog>(), Substitute.For<IMainlineVersionCalculator>(),
            repositoryStoreMock, new(context), versionStrategies, effectiveBranchConfigurationFinderMock, incrementStrategyFinderMock);

        // Act
        var nextVersion = unitUnderTest.FindVersion();

        // Assert
        nextVersion.BaseVersion.Source.ShouldNotBe(higherVersion.Source);
        nextVersion.BaseVersion.SemanticVersion.ShouldNotBe(higherVersion.SemanticVersion);
        nextVersion.BaseVersion.Source.ShouldBe(lowerVersion.Source);
        nextVersion.BaseVersion.SemanticVersion.ShouldBe(lowerVersion.SemanticVersion);
    }

    [Test]
    public void ShouldIgnorePreReleaseVersionInMainlineMode()
    {
        // Arrange
        var branchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());
        var fakeIgnoreConfig = new TestIgnoreConfig(new ExcludeSourcesContainingExclude());
        var configuration = TestConfigurationBuilder.New.WithIgnoreConfig(fakeIgnoreConfig).WithVersioningMode(VersioningMode.Mainline).Build();
        var context = new GitVersionContext(branchMock, null, configuration, null, 0);
        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
        var effectiveConfiguration = context.GetEffectiveConfiguration(branchMock);
        var effectiveBranchConfiguration = new EffectiveBranchConfiguration(branchMock, effectiveConfiguration);
        var effectiveBranchConfigurationFinderMock = Substitute.For<IEffectiveBranchConfigurationFinder>();
        effectiveBranchConfigurationFinderMock.GetConfigurations(branchMock, configuration).Returns(new[] { effectiveBranchConfiguration });
        var incrementStrategyFinderMock = Substitute.For<IIncrementStrategyFinder>();
        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(Enumerable.Empty<IBranch>());
        var lowerVersion = new BaseVersion("dummy", false, new SemanticVersion(1), GitToolsTestingExtensions.CreateMockCommit(), null);
        var preReleaseVersion = new BaseVersion(
            "prerelease",
            false,
            new SemanticVersion(1, 0, 1)
            {
                PreReleaseTag = new SemanticVersionPreReleaseTag
                {
                    Name = "alpha",
                    Number = 1
                }
            },
            GitToolsTestingExtensions.CreateMockCommit(),
            null
        );
        var mainlineVersionCalculatorMock = Substitute.For<IMainlineVersionCalculator>();
        mainlineVersionCalculatorMock.FindMainlineModeVersion(Arg.Any<BaseVersion>()).Returns(lowerVersion.SemanticVersion);
        var versionStrategies = new IVersionStrategy[] { new TestVersionStrategy(preReleaseVersion, lowerVersion) };
        var unitUnderTest = new NextVersionCalculator(Substitute.For<ILog>(), mainlineVersionCalculatorMock,
            repositoryStoreMock, new(context), versionStrategies, effectiveBranchConfigurationFinderMock, incrementStrategyFinderMock);

        // Act
        var nextVersion = unitUnderTest.FindVersion();

        // Assert
        nextVersion.BaseVersion.Source.ShouldNotBe(preReleaseVersion.Source);
        nextVersion.BaseVersion.SemanticVersion.ShouldNotBe(preReleaseVersion.SemanticVersion);
        nextVersion.BaseVersion.Source.ShouldBe(lowerVersion.Source);
        nextVersion.BaseVersion.SemanticVersion.ShouldBe(lowerVersion.SemanticVersion);
    }

    private class TestIgnoreConfig : IgnoreConfig
    {
        private readonly IVersionFilter filter;

        public override bool IsEmpty => false;

        public TestIgnoreConfig(IVersionFilter filter) => this.filter = filter;

        public override IEnumerable<IVersionFilter> ToFilters()
        {
            yield return this.filter;
        }
    }

    private class ExcludeSourcesContainingExclude : IVersionFilter
    {
        public bool Exclude(BaseVersion version, out string? reason)
        {
            reason = null;

            if (!version.Source.Contains("exclude"))
                return false;

            reason = "was excluded";
            return true;
        }
    }

    private sealed class V1Strategy : IVersionStrategy
    {
        private readonly ICommit? when;

        public V1Strategy(DateTimeOffset? when)
        {
            if (when != null)
            {
                this.when = GitToolsTestingExtensions.CreateMockCommit();
                this.when.When.Returns(when.Value);
            }
            else
            {
                this.when = null;
            }
        }

        public IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
        {
            yield return new BaseVersion("Source 1", false, new SemanticVersion(1), this.when, null);
        }
    }

    private sealed class V2Strategy : IVersionStrategy
    {
        private readonly ICommit? when;

        public V2Strategy(DateTimeOffset? when)
        {
            if (when != null)
            {
                this.when = GitToolsTestingExtensions.CreateMockCommit();
                this.when.When.Returns(when.Value);
            }
            else
            {
                this.when = null;
            }
        }

        public IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
        {
            yield return new BaseVersion("Source 2", true, new SemanticVersion(2), this.when, null);
        }
    }

    private sealed class TestVersionStrategy : IVersionStrategy
    {
        private readonly IEnumerable<BaseVersion> baseVersions;

        public TestVersionStrategy(params BaseVersion[] baseVersions) => this.baseVersions = baseVersions;

        public IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration) => this.baseVersions;
    }
}
