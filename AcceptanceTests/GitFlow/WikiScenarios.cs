namespace AcceptanceTests.GitFlow
{
    using GitVersion;
    using Helpers;
    using LibGit2Sharp;
    using Shouldly;
    using Xunit;

    public class WikiScenarios
    {
        /// <summary>
        /// https://github.com/Particular/GitVersion/wiki/GitFlowExamples#minor-release
        /// </summary>
        /* These will be slightly out of sync because we need to make a commit to force a merge commit
participant featureAbc
participant develop
participant release-1.3.0
master -> master: tag 1.2.0
master -> develop: merge
note over develop: 1.3.0.0-unstable
develop -> featureAbc: branch
note over featureAbc: Open Pull Request #2
note over featureAbc: 1.3.0-PullRequest.2+0
featureAbc -> featureAbc: commit
note over featureAbc: 1.3.0-PullRequest.2+1
featureAbc -> develop: merge --no-ff
note over develop: 1.3.0.3-unstable
develop -> release-1.3.0: branch
note over release-1.3.0: 1.3.0-beta.1+0
develop -> develop: commit
note over develop: 1.3.0.4-unstable
release-1.3.0 -> release-1.3.0: commit
note over release-1.3.0: 1.3.0-beta.1+1
release-1.3.0 -> release-1.3.0: tag 1.3.0-beta.1
note over release-1.3.0: 1.3.0-beta.2+1
release-1.3.0 -> master: merge and tag master 1.3.0
note over master: 1.3.0
release-1.3.0 -> develop: merge --no-ff
note over develop: 1.4.0.2-unstable
         */
        [Fact]
        public void MinorReleaseExample()
        {
            using (var fixture = new EmptyRepositoryFixture())
            {
                fixture.Repository.MakeATaggedCommit("1.2.0");

                // Branch to develop
                fixture.Repository.CreateBranch("develop").Checkout();
                fixture.ExecuteGitVersion().OutputVariables[VariableProvider.FullSemVer].ShouldBe("1.3.0.0-unstable");

                // Open Pull Request
                fixture.Repository.CreateBranch("pull/2/merge").Checkout();
                fixture.ExecuteGitVersion().OutputVariables[VariableProvider.FullSemVer].ShouldBe("1.3.0-PullRequest.2+0");
                fixture.Repository.MakeACommit();
                fixture.ExecuteGitVersion().OutputVariables[VariableProvider.FullSemVer].ShouldBe("1.3.0-PullRequest.2+1");

                // Merge into develop
                fixture.Repository.Checkout("develop");
                fixture.Repository.MergeNoFF("pull/2/merge", Constants.SignatureNow());
                fixture.ExecuteGitVersion().OutputVariables[VariableProvider.FullSemVer].ShouldBe("1.3.0.2-unstable");

                // Create release branch
                fixture.Repository.CreateBranch("release-1.3.0").Checkout();
                fixture.ExecuteGitVersion().OutputVariables[VariableProvider.FullSemVer].ShouldBe("1.3.0-beta.1+0");

                // Make another commit on develop
                fixture.Repository.Checkout("develop");
                fixture.Repository.MakeACommit();
                fixture.ExecuteGitVersion().OutputVariables[VariableProvider.FullSemVer].ShouldBe("1.3.0.3-unstable");

                // Make a commit to release-1.3.0
                fixture.Repository.Checkout("release-1.3.0");
                fixture.Repository.MakeACommit();
                fixture.ExecuteGitVersion().OutputVariables[VariableProvider.FullSemVer].ShouldBe("1.3.0-beta.1+1");

                // Apply beta.0 tag
                fixture.Repository.ApplyTag("1.3.0-beta.1");
                fixture.ExecuteGitVersion().OutputVariables[VariableProvider.FullSemVer].ShouldBe("1.3.0-beta.2+1");

                // Merge release branch to master
                fixture.Repository.Checkout("master");
                fixture.Repository.MergeNoFF("release-1.3.0", Constants.SignatureNow());
                fixture.ExecuteGitVersion().OutputVariables[VariableProvider.FullSemVer].ShouldBe("1.3.0");
                fixture.Repository.ApplyTag("1.3.0");

                // Verify develop version
                fixture.Repository.Checkout("develop");
                fixture.Repository.MergeNoFF("release-1.3.0", Constants.SignatureNow());
                fixture.ExecuteGitVersion().OutputVariables[VariableProvider.FullSemVer].ShouldBe("1.4.0.2-unstable");
            }
        }
    }
}