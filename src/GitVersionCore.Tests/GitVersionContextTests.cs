namespace GitVersionCore.Tests
{
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

            var mockBranch = new MockBranch("master") { new MockCommit { CommitterEx = SignatureBuilder.SignatureNow() } };
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
                        "develop", new BranchConfig
                        {
                            VersioningMode = VersioningMode.ContinuousDeployment,
                            Tag = "alpha"
                        }
                    }
                }
            };
            ConfigurationProvider.ApplyDefaultsTo(config);
            var develop = new MockBranch("develop") { new MockCommit { CommitterEx = SignatureBuilder.SignatureNow() } };
            var mockRepository = new MockRepository
            {
                Branches = new MockBranchCollection
                {
                    new MockBranch("master") { new MockCommit { CommitterEx = SignatureBuilder.SignatureNow() } },
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
                    { "develop", new BranchConfig { Increment = IncrementStrategy.Major} },
                    { "feature[/-]", new BranchConfig { Increment = IncrementStrategy.Inherit} }
                }
            };

            using (var repo = new EmptyRepositoryFixture(config))
            {
                repo.Repository.MakeACommit();
                repo.Repository.CreateBranch("develop").Checkout();
                repo.Repository.MakeACommit();
                var featureBranch = repo.Repository.CreateBranch("feature/foo");
                featureBranch.Checkout();
                repo.Repository.MakeACommit();

                var context = new GitVersionContext(repo.Repository, config);
                context.Configuration.Increment.ShouldBe(IncrementStrategy.Major);
            }
        }
    }
}