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

        [Test]
        public void UsesBranchSpecificConfigOverTopLevelDefaults()
        {
            var config = new Config
            {
                VersioningMode = VersioningMode.ContinuousDelivery,
                Branches =
                {
                    {
                        "dev(elop)?(ment)?$", new BranchConfig
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
        public void CanFindParentBranchForInheritingIncrementStrategy()
        {
            var config = new Config
            {
                Branches =
                {
                    { "dev(elop)?(ment)?$", new BranchConfig { Increment = IncrementStrategy.Major} },
                    { "features?[/-]", new BranchConfig { Increment = IncrementStrategy.Inherit} }
                }
            }.ApplyDefaults();

            using (var repo = new EmptyRepositoryFixture())
            {
                repo.Repository.MakeACommit();
                repo.Repository.Checkout(repo.Repository.CreateBranch("develop"));
                repo.Repository.MakeACommit();
                var featureBranch = repo.Repository.CreateBranch("feature/foo");
                repo.Repository.Checkout(featureBranch);
                repo.Repository.MakeACommit();

                var context = new GitVersionContext(repo.Repository, config);
                context.Configuration.Increment.ShouldBe(IncrementStrategy.Major);
            }
        }
    }
}