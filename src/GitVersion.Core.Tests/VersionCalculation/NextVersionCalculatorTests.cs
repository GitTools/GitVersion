//using GitVersion.Common;
//using GitVersion.Configuration;
//using GitVersion.Core.Tests.Helpers;
//using GitVersion.Core.Tests.IntegrationTests;
//using GitVersion.Logging;
//using GitVersion.VersionCalculation;
//using LibGit2Sharp;
//using Microsoft.Extensions.DependencyInjection;

//namespace GitVersion.Core.Tests.VersionCalculation;

//public class NextVersionCalculatorTests : TestBase
//{
//    [Test]
//    public void ShouldIncrementVersionBasedOnConfig()
//    {
//        var contextBuilder = new GitVersionContextBuilder();

//        contextBuilder.Build();

//        contextBuilder.ServicesProvider.ShouldNotBeNull();
//        var nextVersionCalculator = contextBuilder.ServicesProvider.GetRequiredService<INextVersionCalculator>();
//        nextVersionCalculator.ShouldNotBeNull();

//        var nextVersion = nextVersionCalculator.FindVersion();

//        nextVersion.IncrementedVersion.ToString().ShouldBe("0.0.1");
//    }

//    [Test]
//    public void DoesNotIncrementWhenBaseVersionSaysNotTo()
//    {
//        var contextBuilder = new GitVersionContextBuilder();

//        var overrideConfiguration = new Dictionary<object, object?>()
//        {
//            { "next-version", "1.0.0" }
//        };
//        contextBuilder.WithOverrideConfiguration(overrideConfiguration).Build();

//        contextBuilder.ServicesProvider.ShouldNotBeNull();
//        var nextVersionCalculator = contextBuilder.ServicesProvider.GetRequiredService<INextVersionCalculator>();

//        nextVersionCalculator.ShouldNotBeNull();

//        var nextVersion = nextVersionCalculator.FindVersion();

//        nextVersion.IncrementedVersion.ToString().ShouldBe("1.0.0");
//    }

//    [Test]
//    public void AppliesBranchPreReleaseTag()
//    {
//        var contextBuilder = new GitVersionContextBuilder();

//        contextBuilder.WithDevelopBranch().Build();

//        contextBuilder.ServicesProvider.ShouldNotBeNull();
//        var nextVersionCalculator = contextBuilder.ServicesProvider.GetRequiredService<INextVersionCalculator>();
//        nextVersionCalculator.ShouldNotBeNull();

//        var nextVersion = nextVersionCalculator.FindVersion();

//        nextVersion.IncrementedVersion.ToString("f").ShouldBe("0.1.0-alpha.1+0");
//    }

//    [Test]
//    public void PreReleaseLabelCanUseBranchName()
//    {
//        var configuration = GitFlowConfigurationBuilder.New
//            .WithNextVersion("1.0.0")
//            .WithBranch("custom", builder => builder
//                .WithRegularExpression("custom/")
//                .WithLabel(ConfigurationConstants.BranchNamePlaceholder)
//                .WithSourceBranches()
//            )
//            .Build();

//        using var fixture = new EmptyRepositoryFixture();
//        fixture.MakeACommit();
//        fixture.BranchTo("develop");
//        fixture.MakeACommit();
//        fixture.BranchTo("custom/foo");
//        fixture.MakeACommit();

//        fixture.AssertFullSemver("1.0.0-foo.1+3", configuration);
//    }

//    [Test]
//    public void PreReleaseVersionMainline()
//    {
//        var configuration = GitFlowConfigurationBuilder.New
//            .WithNextVersion("1.0.0")
//            .WithBranch("unknown", builder => builder.WithVersioningMode(VersioningMode.Mainline))
//            .WithBranch("main", builder => builder.WithVersioningMode(VersioningMode.Mainline))
//            .Build();

//        using var fixture = new EmptyRepositoryFixture();
//        fixture.MakeACommit();
//        fixture.BranchTo("foo");
//        fixture.MakeACommit();

//        fixture.AssertFullSemver("1.0.0-foo.1", configuration);
//    }

//    [Test]
//    public void MergeIntoMainline()
//    {
//        var configuration = GitFlowConfigurationBuilder.New
//            .WithNextVersion("1.0.0")
//            .WithBranch("main", builder => builder.WithVersioningMode(VersioningMode.Mainline))
//            .Build();

//        using var fixture = new EmptyRepositoryFixture();
//        fixture.MakeACommit();
//        fixture.BranchTo("foo");
//        fixture.MakeACommit();
//        fixture.Checkout(MainBranch);
//        fixture.MergeNoFF("foo");

//        fixture.AssertFullSemver("1.0.0", configuration);
//    }

//    [Test]
//    public void MergeFeatureIntoMainline()
//    {
//        var configuration = GitFlowConfigurationBuilder.New
//            .WithBranch("main", builder => builder.WithVersioningMode(VersioningMode.Mainline))
//            .WithBranch("feature", builder => builder.WithVersioningMode(VersioningMode.Mainline))
//            .Build();

//        using var fixture = new EmptyRepositoryFixture();
//        fixture.MakeACommit();
//        fixture.ApplyTag("1.0.0");
//        fixture.AssertFullSemver("1.0.0", configuration);

//        fixture.BranchTo("feature/foo");
//        fixture.MakeACommit();
//        fixture.AssertFullSemver("1.0.1-foo.1", configuration);
//        fixture.ApplyTag("1.0.1-foo.1");

//        fixture.Checkout(MainBranch);
//        fixture.MergeNoFF("feature/foo");
//        fixture.AssertFullSemver("1.0.1", configuration);
//    }

//    [Test]
//    public void MergeFeatureIntoMainlineWithMinorIncrement()
//    {
//        var configuration = GitFlowConfigurationBuilder.New
//            .WithBranch("main", builder => builder.WithVersioningMode(VersioningMode.Mainline))
//            .WithBranch("feature", builder => builder
//                .WithVersioningMode(VersioningMode.Mainline)
//                .WithIncrement(IncrementStrategy.Minor)
//            )
//            .Build();

//        using var fixture = new EmptyRepositoryFixture();
//        fixture.MakeACommit();
//        fixture.ApplyTag("1.0.0");
//        fixture.AssertFullSemver("1.0.0", configuration);

//        fixture.BranchTo("feature/foo");
//        fixture.MakeACommit();
//        fixture.AssertFullSemver("1.1.0-foo.1", configuration);
//        fixture.ApplyTag("1.1.0-foo.1");

//        fixture.Checkout(MainBranch);
//        fixture.MergeNoFF("feature/foo");
//        fixture.AssertFullSemver("1.1.0", configuration);
//    }

//    [Test]
//    public void MergeFeatureIntoMainlineWithMinorIncrementAndThenMergeHotfix()
//    {
//        var configuration = GitFlowConfigurationBuilder.New
//            .WithVersioningMode(VersioningMode.Mainline)
//            .WithBranch("feature", builder => builder
//                .WithIncrement(IncrementStrategy.Minor)
//                .WithVersioningMode(VersioningMode.Mainline))
//            .WithBranch("hotfix", builder => builder.WithVersioningMode(VersioningMode.Mainline))
//            .Build();

//        using var fixture = new EmptyRepositoryFixture();
//        fixture.MakeACommit();
//        fixture.ApplyTag("1.0.0");
//        fixture.AssertFullSemver("1.0.0", configuration);

//        fixture.BranchTo("feature/foo");
//        fixture.MakeACommit();
//        fixture.AssertFullSemver("1.1.0-foo.1", configuration);
//        fixture.ApplyTag("1.1.0-foo.1");

//        fixture.Checkout(MainBranch);
//        fixture.MergeNoFF("feature/foo");
//        fixture.AssertFullSemver("1.1.0", configuration);
//        fixture.ApplyTag("1.1.0");

//        fixture.BranchTo("hotfix/bar");
//        fixture.MakeACommit();
//        fixture.AssertFullSemver("1.1.1-beta.1", configuration);
//        fixture.ApplyTag("1.1.1-beta.1");

//        fixture.Checkout(MainBranch);
//        fixture.MergeNoFF("hotfix/bar");
//        fixture.AssertFullSemver("1.1.1", configuration);
//    }

//    [Test]
//    public void PreReleaseLabelCanUseBranchNameVariable()
//    {
//        var configuration = GitFlowConfigurationBuilder.New
//            .WithNextVersion("1.0.0")
//            .WithBranch("custom", builder => builder
//                .WithRegularExpression("custom/")
//                .WithLabel($"alpha.{ConfigurationConstants.BranchNamePlaceholder}")
//                .WithSourceBranches()
//            )
//            .Build();

//        using var fixture = new EmptyRepositoryFixture();
//        fixture.MakeACommit();
//        fixture.BranchTo("develop");
//        fixture.MakeACommit();
//        fixture.BranchTo("custom/foo");
//        fixture.MakeACommit();

//        fixture.AssertFullSemver("1.0.0-alpha.foo.1+3", configuration);
//    }

//    [Test]
//    public void PreReleaseNumberShouldBeScopeToPreReleaseLabelInContinuousDelivery()
//    {
//        var configuration = GitFlowConfigurationBuilder.New
//            .WithBranch("main", builder => builder.WithLabel("beta"))
//            .WithBranch("feature", builder => builder
//                .WithVersioningMode(VersioningMode.ContinuousDelivery)
//            )
//            .Build();

//        using var fixture = new EmptyRepositoryFixture();
//        fixture.MakeACommit();

//        fixture.BranchTo("feature/test");
//        fixture.MakeATaggedCommit("0.1.0-test.1");
//        fixture.MakeACommit();

//        fixture.AssertFullSemver("0.1.0-test.2+1", configuration);

//        fixture.Checkout("main");
//        fixture.Repository.Merge("feature/test", Generate.SignatureNow());

//        fixture.AssertFullSemver("0.1.0-beta.1+3", configuration);
//    }

//    [Test]
//    public void GetNextVersionOnNonMainlineBranchWithoutCommitsShouldWorkNormally()
//    {
//        var configuration = GitFlowConfigurationBuilder.New
//            .WithNextVersion("1.0.0")
//            .WithBranch("feature", builder => builder.WithVersioningMode(VersioningMode.Mainline))
//            .Build();

//        using var fixture = new EmptyRepositoryFixture();
//        fixture.MakeACommit("initial commit");
//        fixture.BranchTo("feature/f1");
//        fixture.AssertFullSemver("1.0.0-f1.0", configuration);
//    }

//    [Test]
//    public void ChoosesHighestVersionReturnedFromStrategies()
//    {
//        // Arrange
//        var branchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());
//        var configuration = GitFlowConfigurationBuilder.New.Build();
//        var context = new GitVersionContext(branchMock, null, configuration, null, 0);
//        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
//        var effectiveConfiguration = context.Configuration.GetEffectiveConfiguration(branchMock);
//        var effectiveBranchConfiguration = new EffectiveBranchConfiguration(branchMock, effectiveConfiguration);
//        var effectiveBranchConfigurationFinderMock = Substitute.For<IEffectiveBranchConfigurationFinder>();
//        effectiveBranchConfigurationFinderMock.GetConfigurations(branchMock, configuration).Returns(new[] { effectiveBranchConfiguration });
//        var incrementStrategyFinderMock = Substitute.For<IIncrementStrategyFinder>();
//        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(Enumerable.Empty<IBranch>());
//        var dateTimeOffset = DateTimeOffset.Now;
//        var versionStrategies = new IVersionStrategy[] { new V1Strategy(DateTimeOffset.Now), new V2Strategy(dateTimeOffset) };
//        var mainlineVersionCalculatorMock = Substitute.For<IMainlineVersionCalculator>();
//        mainlineVersionCalculatorMock.CreateVersionBuildMetaData(Arg.Any<ICommit?>()).Returns(SemanticVersionBuildMetaData.Empty);
//        var continuousDeploymentVersionCalculatorMock = Substitute.For<IContinuousDeploymentVersionCalculator>();

//        var unitUnderTest = new NextVersionCalculator(
//            Substitute.For<ILog>(),
//            mainlineVersionCalculator: mainlineVersionCalculatorMock,
//            continuousDeploymentVersionCalculator: continuousDeploymentVersionCalculatorMock,
//            repositoryStoreMock, new(context),
//            versionStrategies, effectiveBranchConfigurationFinderMock,
//            incrementStrategyFinderMock
//        );

//        // Act
//        var nextVersion = unitUnderTest.FindVersion();

//        // Assert
//        nextVersion.BaseVersion.SemanticVersion.ToString().ShouldBe("2.0.0");
//        nextVersion.BaseVersion.ShouldIncrement.ShouldBe(true);
//        nextVersion.BaseVersion.BaseVersionSource.ShouldNotBeNull();
//        nextVersion.BaseVersion.BaseVersionSource.When.ShouldBe(dateTimeOffset);
//    }

//    [Test]
//    public void UsesWhenFromNextBestMatchIfHighestDoesntHaveWhen()
//    {
//        // Arrange
//        var branchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());
//        var configuration = GitFlowConfigurationBuilder.New.Build();
//        var context = new GitVersionContext(branchMock, null, configuration, null, 0);
//        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
//        var effectiveConfiguration = context.Configuration.GetEffectiveConfiguration(branchMock);
//        var effectiveBranchConfiguration = new EffectiveBranchConfiguration(branchMock, effectiveConfiguration);
//        var effectiveBranchConfigurationFinderMock = Substitute.For<IEffectiveBranchConfigurationFinder>();
//        effectiveBranchConfigurationFinderMock.GetConfigurations(branchMock, configuration).Returns(new[] { effectiveBranchConfiguration });
//        var incrementStrategyFinderMock = Substitute.For<IIncrementStrategyFinder>();
//        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(Enumerable.Empty<IBranch>());
//        var when = DateTimeOffset.Now;
//        var versionStrategies = new IVersionStrategy[] { new V1Strategy(when), new V2Strategy(null) };
//        var mainlineVersionCalculatorMock = Substitute.For<IMainlineVersionCalculator>();
//        mainlineVersionCalculatorMock.CreateVersionBuildMetaData(Arg.Any<ICommit?>()).Returns(SemanticVersionBuildMetaData.Empty);
//        var continuousDeploymentVersionCalculatorMock = Substitute.For<IContinuousDeploymentVersionCalculator>();

//        var unitUnderTest = new NextVersionCalculator(
//            Substitute.For<ILog>(),
//            mainlineVersionCalculator: mainlineVersionCalculatorMock,
//            continuousDeploymentVersionCalculator: continuousDeploymentVersionCalculatorMock,
//            repositoryStoreMock, new(context),
//            versionStrategies, effectiveBranchConfigurationFinderMock,
//            incrementStrategyFinderMock
//        );

//        // Act
//        var nextVersion = unitUnderTest.FindVersion();

//        // Assert
//        nextVersion.BaseVersion.SemanticVersion.ToString().ShouldBe("2.0.0");
//        nextVersion.BaseVersion.ShouldIncrement.ShouldBe(true);
//        nextVersion.BaseVersion.BaseVersionSource.ShouldNotBeNull();
//        nextVersion.BaseVersion.BaseVersionSource.When.ShouldBe(when);
//    }

//    [Test]
//    public void UsesWhenFromNextBestMatchIfHighestDoesntHaveWhenReversedOrder()
//    {
//        // Arrange
//        var branchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());
//        var configuration = GitFlowConfigurationBuilder.New.Build();
//        var context = new GitVersionContext(branchMock, null, configuration, null, 0);
//        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
//        var effectiveConfiguration = context.Configuration.GetEffectiveConfiguration(branchMock);
//        var effectiveBranchConfiguration = new EffectiveBranchConfiguration(branchMock, effectiveConfiguration);
//        var effectiveBranchConfigurationFinderMock = Substitute.For<IEffectiveBranchConfigurationFinder>();
//        effectiveBranchConfigurationFinderMock.GetConfigurations(branchMock, configuration).Returns(new[] { effectiveBranchConfiguration });
//        var incrementStrategyFinderMock = Substitute.For<IIncrementStrategyFinder>();
//        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(Enumerable.Empty<IBranch>());
//        var when = DateTimeOffset.Now;
//        var versionStrategies = new IVersionStrategy[] { new V2Strategy(null), new V1Strategy(when) };
//        var mainlineVersionCalculatorMock = Substitute.For<IMainlineVersionCalculator>();
//        mainlineVersionCalculatorMock.CreateVersionBuildMetaData(Arg.Any<ICommit?>()).Returns(SemanticVersionBuildMetaData.Empty);
//        var continuousDeploymentVersionCalculatorMock = Substitute.For<IContinuousDeploymentVersionCalculator>();

//        var unitUnderTest = new NextVersionCalculator(
//            Substitute.For<ILog>(),
//            mainlineVersionCalculator: mainlineVersionCalculatorMock,
//            continuousDeploymentVersionCalculator: continuousDeploymentVersionCalculatorMock,
//            repositoryStoreMock, new(context),
//            versionStrategies, effectiveBranchConfigurationFinderMock,
//            incrementStrategyFinderMock
//        );

//        // Act
//        var nextVersion = unitUnderTest.FindVersion();

//        // Assert
//        nextVersion.BaseVersion.SemanticVersion.ToString().ShouldBe("2.0.0");
//        nextVersion.BaseVersion.ShouldIncrement.ShouldBe(true);
//        nextVersion.BaseVersion.BaseVersionSource.ShouldNotBeNull();
//        nextVersion.BaseVersion.BaseVersionSource.When.ShouldBe(when);
//    }

//    [Test]
//    public void ShouldNotFilterVersion()
//    {
//        // Arrange
//        var branchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());
//        var configuration = GitFlowConfigurationBuilder.New.Build();
//        var context = new GitVersionContext(branchMock, null, configuration, null, 0);
//        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
//        var effectiveConfiguration = context.Configuration.GetEffectiveConfiguration(branchMock);
//        var effectiveBranchConfiguration = new EffectiveBranchConfiguration(branchMock, effectiveConfiguration);
//        var effectiveBranchConfigurationFinderMock = Substitute.For<IEffectiveBranchConfigurationFinder>();
//        effectiveBranchConfigurationFinderMock.GetConfigurations(branchMock, configuration).Returns(new[] { effectiveBranchConfiguration });
//        var incrementStrategyFinderMock = Substitute.For<IIncrementStrategyFinder>();
//        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(Enumerable.Empty<IBranch>());
//        var version = new BaseVersion("dummy", false, new SemanticVersion(2), GitToolsTestingExtensions.CreateMockCommit(), null);
//        var versionStrategies = new IVersionStrategy[] { new TestVersionStrategy(version) };
//        var mainlineVersionCalculatorMock = Substitute.For<IMainlineVersionCalculator>();
//        mainlineVersionCalculatorMock.CreateVersionBuildMetaData(Arg.Any<ICommit?>()).Returns(SemanticVersionBuildMetaData.Empty);
//        var continuousDeploymentVersionCalculatorMock = Substitute.For<IContinuousDeploymentVersionCalculator>();

//        var unitUnderTest = new NextVersionCalculator(
//            Substitute.For<ILog>(),
//            mainlineVersionCalculator: mainlineVersionCalculatorMock,
//            continuousDeploymentVersionCalculator: continuousDeploymentVersionCalculatorMock,
//            repositoryStoreMock, new(context),
//            versionStrategies, effectiveBranchConfigurationFinderMock,
//            incrementStrategyFinderMock
//        );

//        // Act
//        var nextVersion = unitUnderTest.FindVersion();

//        // Assert
//        nextVersion.BaseVersion.Source.ShouldBe(version.Source);
//        nextVersion.BaseVersion.ShouldIncrement.ShouldBe(version.ShouldIncrement);
//        nextVersion.BaseVersion.SemanticVersion.ShouldBe(version.SemanticVersion);
//    }

//    [Test]
//    public void ShouldFilterVersion()
//    {
//        // Arrange
//        var commitToExclude = GitToolsTestingExtensions.CreateMockCommit();
//        var branchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());
//        var ignoreConfiguration = IgnoreConfigurationBuilder.New.WithShas(commitToExclude.Sha).Build();
//        var configuration = GitFlowConfigurationBuilder.New.WithIgnoreConfiguration(ignoreConfiguration).Build();
//        var context = new GitVersionContext(branchMock, null, configuration, null, 0);
//        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
//        var effectiveConfiguration = context.Configuration.GetEffectiveConfiguration(branchMock);
//        var effectiveBranchConfiguration = new EffectiveBranchConfiguration(branchMock, effectiveConfiguration);
//        var effectiveBranchConfigurationFinderMock = Substitute.For<IEffectiveBranchConfigurationFinder>();
//        effectiveBranchConfigurationFinderMock.GetConfigurations(branchMock, configuration).Returns(new[] { effectiveBranchConfiguration });
//        var incrementStrategyFinderMock = Substitute.For<IIncrementStrategyFinder>();
//        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(Enumerable.Empty<IBranch>());
//        var higherVersion = new BaseVersion("exclude", false, new SemanticVersion(2), commitToExclude, null);
//        var lowerVersion = new BaseVersion("dummy", false, new SemanticVersion(1), GitToolsTestingExtensions.CreateMockCommit(), null);
//        var versionStrategies = new IVersionStrategy[] { new TestVersionStrategy(higherVersion, lowerVersion) };
//        var mainlineVersionCalculatorMock = Substitute.For<IMainlineVersionCalculator>();
//        mainlineVersionCalculatorMock.CreateVersionBuildMetaData(Arg.Any<ICommit?>()).Returns(SemanticVersionBuildMetaData.Empty);
//        var continuousDeploymentVersionCalculatorMock = Substitute.For<IContinuousDeploymentVersionCalculator>();

//        var unitUnderTest = new NextVersionCalculator(
//            Substitute.For<ILog>(),
//            mainlineVersionCalculator: mainlineVersionCalculatorMock,
//            continuousDeploymentVersionCalculator: continuousDeploymentVersionCalculatorMock,
//            repositoryStoreMock, new(context),
//            versionStrategies, effectiveBranchConfigurationFinderMock,
//            incrementStrategyFinderMock
//        );

//        // Act
//        var nextVersion = unitUnderTest.FindVersion();

//        // Assert
//        nextVersion.BaseVersion.Source.ShouldNotBe(higherVersion.Source);
//        nextVersion.BaseVersion.SemanticVersion.ShouldNotBe(higherVersion.SemanticVersion);
//        nextVersion.BaseVersion.Source.ShouldBe(lowerVersion.Source);
//        nextVersion.BaseVersion.SemanticVersion.ShouldBe(lowerVersion.SemanticVersion);
//    }

//    [Test]
//    public void ShouldIgnorePreReleaseVersionInMainlineMode()
//    {
//        // Arrange
//        var branchMock = GitToolsTestingExtensions.CreateMockBranch("main", GitToolsTestingExtensions.CreateMockCommit());
//        var configuration = GitFlowConfigurationBuilder.New
//            .WithBranch("main", builder => builder.WithVersioningMode(VersioningMode.Mainline))
//            .Build();
//        var context = new GitVersionContext(branchMock, null, configuration, null, 0);
//        var repositoryStoreMock = Substitute.For<IRepositoryStore>();
//        var effectiveConfiguration = context.Configuration.GetEffectiveConfiguration(branchMock);
//        var effectiveBranchConfiguration = new EffectiveBranchConfiguration(branchMock, effectiveConfiguration);
//        var effectiveBranchConfigurationFinderMock = Substitute.For<IEffectiveBranchConfigurationFinder>();
//        effectiveBranchConfigurationFinderMock.GetConfigurations(branchMock, configuration).Returns(new[] { effectiveBranchConfiguration });
//        var incrementStrategyFinderMock = Substitute.For<IIncrementStrategyFinder>();
//        repositoryStoreMock.GetSourceBranches(branchMock, configuration, Arg.Any<HashSet<IBranch>>()).Returns(Enumerable.Empty<IBranch>());
//        var lowerVersion = new BaseVersion("dummy", false, new SemanticVersion(1), GitToolsTestingExtensions.CreateMockCommit(), null);
//        var preReleaseVersion = new BaseVersion(
//            "prerelease",
//            false,
//            new SemanticVersion(1, 0, 1)
//            {
//                PreReleaseTag = new SemanticVersionPreReleaseTag
//                {
//                    Name = "alpha",
//                    Number = 1
//                }
//            },
//            GitToolsTestingExtensions.CreateMockCommit(),
//            null
//        );
//        var mainlineVersionCalculatorMock = Substitute.For<IMainlineVersionCalculator>();
//        mainlineVersionCalculatorMock.FindMainlineModeVersion(Arg.Any<NextVersion>()).Returns(lowerVersion.SemanticVersion);
//        var continuousDeploymentVersionCalculatorMock = Substitute.For<IContinuousDeploymentVersionCalculator>();

//        var versionStrategies = new IVersionStrategy[] { new TestVersionStrategy(preReleaseVersion, lowerVersion) };
//        var unitUnderTest = new NextVersionCalculator(Substitute.For<ILog>(), mainlineVersionCalculatorMock, continuousDeploymentVersionCalculatorMock,
//            repositoryStoreMock, new(context), versionStrategies, effectiveBranchConfigurationFinderMock, incrementStrategyFinderMock);

//        // Act
//        var nextVersion = unitUnderTest.FindVersion();

//        // Assert
//        nextVersion.BaseVersion.Source.ShouldNotBe(preReleaseVersion.Source);
//        nextVersion.BaseVersion.SemanticVersion.ShouldNotBe(preReleaseVersion.SemanticVersion);
//        nextVersion.BaseVersion.Source.ShouldBe(lowerVersion.Source);
//        nextVersion.BaseVersion.SemanticVersion.ShouldBe(lowerVersion.SemanticVersion);
//    }

//    private sealed class V1Strategy : IVersionStrategy
//    {
//        private readonly ICommit? when;

//        public V1Strategy(DateTimeOffset? when)
//        {
//            if (when != null)
//            {
//                this.when = GitToolsTestingExtensions.CreateMockCommit();
//                this.when.When.Returns(when.Value);
//            }
//            else
//            {
//                this.when = null;
//            }
//        }

//        public IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
//        {
//            yield return new BaseVersion("Source 1", false, new SemanticVersion(1), this.when, null);
//        }
//    }

//    private sealed class V2Strategy : IVersionStrategy
//    {
//        private readonly ICommit? when;

//        public V2Strategy(DateTimeOffset? when)
//        {
//            if (when != null)
//            {
//                this.when = GitToolsTestingExtensions.CreateMockCommit();
//                this.when.When.Returns(when.Value);
//            }
//            else
//            {
//                this.when = null;
//            }
//        }

//        public IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
//        {
//            yield return new BaseVersion("Source 2", true, new SemanticVersion(2), this.when, null);
//        }
//    }

//    private sealed class TestVersionStrategy : IVersionStrategy
//    {
//        private readonly IEnumerable<BaseVersion> baseVersions;

//        public TestVersionStrategy(params BaseVersion[] baseVersions) => this.baseVersions = baseVersions;

//        public IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration) => this.baseVersions;
//    }
//}
