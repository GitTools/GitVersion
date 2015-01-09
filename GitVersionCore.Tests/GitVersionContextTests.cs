namespace GitVersionCore.Tests
{
    using GitVersion;
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
                VersioningMode = VersioningMode.ContinuousDelivery
            };
            config.Branches["develop"].VersioningMode = VersioningMode.ContinuousDeployment;
            config.Branches["develop"].Tag = "alpha";
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
            context.Configuration.VersioningMode.ShouldBe(VersioningMode.ContinuousDeployment);
            context.Configuration.Tag.ShouldBe("alpha");
        }
    }
}