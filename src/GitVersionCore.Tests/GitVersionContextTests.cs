namespace GitVersionCore.Tests
{
    using GitTools.Testing;
    using GitVersion;
    using LibGit2Sharp;
    using NUnit.Framework;
    using Shouldly;

    public class GitVersionContextTests
    {
        [Test]
        [Theory]
        public void CanInheritVersioningMode(VersioningMode mode)
        {
            var config = new Config
            {
                VersioningMode = mode
            };
            ConfigurationProvider.ApplyDefaultsTo(config);

            var mockBranch = new MockBranch("master") { new MockCommit { CommitterEx = Generate.SignatureNow() } };
            var mockRepository = new MockRepository
            {
                Branches = new MockBranchCollection
                {
                    mockBranch
                }
            };

            var context = new GitVersionContext(mockRepository, mockBranch, config);
            context.Configuration.VersioningMode.ShouldBe(mode);
        }

        [TestCase(IncrementStrategy.Inherit, IncrementStrategy.Patch)] // Since it inherits, the increment strategy of master is used => Patch
        [TestCase(IncrementStrategy.Patch, null)]
        [TestCase(IncrementStrategy.Major, null)]
        [TestCase(IncrementStrategy.Minor, null)]
        [TestCase(IncrementStrategy.None, null)]
        public void CanInheritIncrement(IncrementStrategy increment, IncrementStrategy? alternateExpected)
        {
            // Dummy branch name to make sure that no default config exists.
            const string dummyBranchName = "dummy";

            var config = new Config
            {
                Increment = increment
            };
            ConfigurationProvider.ApplyDefaultsTo(config);

            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.MakeACommit();
                fixture.BranchTo(dummyBranchName);
                fixture.MakeACommit();

                var context = new GitVersionContext(fixture.Repository, fixture.Repository.Branches[dummyBranchName], config);
                context.Configuration.Increment.ShouldBe(alternateExpected ?? increment);
            }

        }

        [Test]
        public void UsesBranchSpecificConfigOverTopLevelDefaults()
        {
            var config = new Config
            {
                VersioningMode = VersioningMode.ContinuousDelivery,
                Branches =
                {
                    {
                        "develop", new BranchConfig
                        {
                            VersioningMode = VersioningMode.ContinuousDeployment,
                            Tag = "alpha"
                        }
                    }
                }
            };
            ConfigurationProvider.ApplyDefaultsTo(config);
            var develop = new MockBranch("develop") { new MockCommit { CommitterEx = Generate.SignatureNow() } };
            var mockRepository = new MockRepository
            {
                Branches = new MockBranchCollection
                {
                    new MockBranch("master") { new MockCommit { CommitterEx = Generate.SignatureNow() } },
                    develop
                }
            };
            var context = new GitVersionContext(mockRepository, develop, config);
            context.Configuration.Tag.ShouldBe("alpha");
        }

        [Test]
        public void UsesFirstBranchConfigWhenMultipleMatch()
        {
            var config = new Config
            {
                VersioningMode = VersioningMode.ContinuousDelivery,
                Branches =
                {
                    { "release/latest", new BranchConfig { Increment = IncrementStrategy.None, Regex = "release/latest" } },
                    { "release", new BranchConfig { Increment = IncrementStrategy.Patch, Regex = "releases?[/-]" } }
                }
            }.ApplyDefaults();

            var releaseLatestBranch = new MockBranch("release/latest") { new MockCommit { CommitterEx = Generate.SignatureNow() } };
            var releaseVersionBranch = new MockBranch("release/1.0.0") { new MockCommit { CommitterEx = Generate.SignatureNow() } };

            var mockRepository = new MockRepository
            {
                Branches = new MockBranchCollection
                {
                    releaseLatestBranch,
                    releaseVersionBranch
                }
            };

            var latestContext = new GitVersionContext(mockRepository, releaseLatestBranch, config);
            latestContext.Configuration.Increment.ShouldBe(IncrementStrategy.None);

            var versionContext = new GitVersionContext(mockRepository, releaseVersionBranch, config);
            versionContext.Configuration.Increment.ShouldBe(IncrementStrategy.Patch);
        }

        [Test]
        public void CanFindParentBranchForInheritingIncrementStrategy()
        {
            var config = new Config
            {
                Branches =
                {
                    { "develop", new BranchConfig { Increment = IncrementStrategy.Major} },
                    { "feature", new BranchConfig { Increment = IncrementStrategy.Inherit} }
                }
            }.ApplyDefaults();

            using (var repo = new EmptyRepositoryFixture())
            {
                repo.Repository.MakeACommit();
                Commands.Checkout(repo.Repository, repo.Repository.CreateBranch("develop"));
                repo.Repository.MakeACommit();
                var featureBranch = repo.Repository.CreateBranch("feature/foo");
                Commands.Checkout(repo.Repository, featureBranch);
                repo.Repository.MakeACommit();

                var context = new GitVersionContext(repo.Repository, config);
                context.Configuration.Increment.ShouldBe(IncrementStrategy.Major);
            }
        }
    }
}