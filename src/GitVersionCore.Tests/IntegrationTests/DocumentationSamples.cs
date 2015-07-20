using GitVersion;
using NUnit.Framework;

[TestFixture]
public class DocumentationSamples
{
    [Test]
    public void GitFlowExample()
    {
        using (var fixture = new EmptyRepositoryFixture(new Config()))
        {
            fixture.Participant("pull/2/merge", "feature");
            fixture.Participant("develop");
            fixture.Participant("release/2.0.0", "majorRelease");
            fixture.Participant("release/1.3.0", "minorRelease");
            fixture.Participant("master");
            fixture.Activate("develop");
            fixture.Activate("master");

            fixture.MakeATaggedCommit("1.2.0");

            /****************************************************
            *             BEGIN Feature branch section          *
            ****************************************************/
            fixture.Divider("Feature branch");
            fixture.NoteOver("Feature branches are likely pushed to a fork,\r\nthen submit a PR." +
                             "pull/2/merge is what your build server sees when\r\n" +
                             "you submit a PR with #2",
                             "feature", "master");

            // Branch to develop
            fixture.BranchTo("develop");
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.3.0-unstable.1");

            // Open Pull Request
            fixture.BranchTo("pull/2/merge");
            fixture.Activate("pull/2/merge");
            fixture.AssertFullSemver("1.3.0-PullRequest.2+1");
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.3.0-PullRequest.2+2");

            // Merge into develop
            fixture.Checkout("develop");
            fixture.MergeNoFF("pull/2/merge");
            fixture.Destroy("pull/2/merge");
            fixture.NoteOver("Feature branches/pr's should\r\n" +
                             "be deleted once merged", "pull/2/merge");
            fixture.AssertFullSemver("1.3.0-unstable.3");
            /****************************************************
            *               END Feature branch section          *
            ****************************************************/

            /****************************************************
            *            BEGIN Hotfix branch section            *
            ****************************************************/
            fixture.Divider("Hotfix release");
            fixture.NoteOver("Hotfix branches are short lived branches\r\n" +
                             "which allow you to do SemVer patch releases",
                             "feature", "master");

            // Create hotfix branch
            fixture.Checkout("master");
            fixture.BranchTo("hotfix/1.2.1", "hotfix");
            fixture.Activate("hotfix/1.2.1");
            fixture.MakeACommit();
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.2.1-beta.1+2");

            // Apply beta.1 tag should be exact tag
            fixture.ApplyTag("1.2.1-beta.1");
            fixture.AssertFullSemver("1.2.1-beta.1");
            fixture.Checkout("master");
            fixture.MergeNoFF("hotfix/1.2.1");
            fixture.Destroy("hotfix/1.2.1");
            fixture.NoteOver("Hotfix branches are deleted once merged", "hotfix/1.2.1");
            fixture.ApplyTag("1.2.1");

            /****************************************************
            *            END Hotfix branch section              *
            ****************************************************/

            /****************************************************
            *         BEGIN Minor Release branch section        *
            ****************************************************/
            fixture.Divider("Minor Release");
            fixture.NoteOver("In GitFlow the release branch is taken from develop",
                             "feature", "master");

            // Create release branch
            fixture.Checkout("develop");
            fixture.BranchTo("release/1.3.0");
            fixture.Activate("release/1.3.0");
            fixture.AssertFullSemver("1.3.0-beta.1+0");

            // Make another commit on develop
            fixture.Checkout("develop");
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.3.0-unstable.4");

            // Make a commit to release-1.3.0
            fixture.Checkout("release/1.3.0");
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.3.0-beta.1+1");

            // Apply beta.1 tag should be exact tag
            fixture.ApplyTag("1.3.0-beta.1");
            fixture.AssertFullSemver("1.3.0-beta.1");

            // Make a commit after a tag should bump up the beta
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.3.0-beta.2+2");

            // Complete release
            fixture.Checkout("master");
            fixture.MergeNoFF("release/1.3.0");
            fixture.AssertFullSemver("1.3.0+0");
            fixture.ApplyTag("1.3.0");
            fixture.AssertFullSemver("1.3.0");
            fixture.Checkout("develop");
            fixture.MergeNoFF("release/1.3.0");
            fixture.NoteOver("Release branches are deleted once merged", "release/1.3.0");
            // Not 0 for commit count as we can't know the increment rules of the merged branch
            fixture.AssertFullSemver("1.4.0-unstable.2");
            /****************************************************
            *         END Minor Release branch section          *
            ****************************************************/

            /****************************************************
            *         BEGIN Major Release branch section        *
            ****************************************************/
            fixture.Divider("Major Release");

            // Create release branch
            fixture.BranchTo("release/2.0.0");
            fixture.AssertFullSemver("2.0.0-beta.1+0");

            // Make another commit on develop
            fixture.Checkout("develop");
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.4.0-unstable.3");

            // Make a commit to release-2.0.0
            fixture.Checkout("release/2.0.0");
            fixture.MakeACommit();
            fixture.AssertFullSemver("2.0.0-beta.1+1");

            // Apply beta.1 tag should be exact tag
            fixture.ApplyTag("2.0.0-beta.1");
            fixture.AssertFullSemver("2.0.0-beta.1");

            // Make a commit after a tag should bump up the beta
            fixture.MakeACommit();
            fixture.AssertFullSemver("2.0.0-beta.2+2");

            // Complete release
            fixture.Checkout("master");
            fixture.MergeNoFF("release/2.0.0");
            fixture.AssertFullSemver("2.0.0+0");
            fixture.ApplyTag("2.0.0");
            fixture.AssertFullSemver("2.0.0");
            fixture.Checkout("develop");
            fixture.MergeNoFF("release/2.0.0");
            fixture.NoteOver("Release branches are deleted once merged", "release/2.0.0");
            // Not 0 for commit count as we can't know the increment rules of the merged branch
            fixture.AssertFullSemver("2.1.0-unstable.2");
            /****************************************************
            *         End Major Release branch section        *
            ****************************************************/

            /****************************************************
            *           BEGIN Support branch section            *
            ****************************************************/
            fixture.Divider("Support Branches");
            fixture.NoteOver("Support branches allow you to create stable releases of a previous major or minor release.\r\n" +
                             "A support branch is essentially master for an old release",
                             "feature", "hotfix/1.2.1");

            // Create hotfix branch
            fixture.Checkout("1.3.0");
            fixture.BranchToFromTag("support/1.3.0", "1.3.0", "master", "support");
            fixture.Activate("support/1.3.0");
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.3.0+1");

            fixture.BranchTo("hotfix/1.3.1", "hotfix2");
            fixture.Activate("hotfix/1.3.1");
            fixture.MakeACommit();
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.3.1-beta.1+3");

            // Apply beta.1 tag should be exact tag
            fixture.ApplyTag("1.3.1-beta.1");
            fixture.AssertFullSemver("1.3.1-beta.1");
            fixture.Checkout("support/1.3.0");
            fixture.MergeNoFF("hotfix/1.3.1");
            fixture.Destroy("hotfix/1.3.1");
            fixture.NoteOver("Hotfix branches are deleted once merged", "hotfix/1.3.1");
            fixture.AssertFullSemver("1.3.1+4");
            fixture.ApplyTag("1.3.1");
            fixture.AssertFullSemver("1.3.1");

            /****************************************************
            *            END Hotfix branch section              *
            ****************************************************/

            /****************************************************
            *           BEGIN Minor support release             *
            ****************************************************/
            fixture.Divider("Minor release via support");
            fixture.NoteOver("Much like hotfixing an old version you can release minor versions of old\r\n" + 
                             "releases using support branches",
                             "feature", "hotfix/1.3.1");

            // Create hotfix branch
            fixture.Checkout("support/1.3.0");

            fixture.BranchTo("release/1.4.0", "supportRelease");
            fixture.Activate("release/1.4.0");
            fixture.MakeACommit();
            fixture.MakeACommit();
            fixture.AssertFullSemver("1.4.0-beta.1+2");

            // Apply beta.1 tag should be exact tag
            fixture.ApplyTag("1.4.0-beta.1");
            fixture.AssertFullSemver("1.4.0-beta.1");
            fixture.Checkout("support/1.3.0");
            fixture.MergeNoFF("release/1.4.0");
            fixture.Destroy("release/1.4.0");
            fixture.NoteOver("Release branches are deleted once merged", "release/1.4.0");
            fixture.AssertFullSemver("1.4.0+0");
            fixture.ApplyTag("1.4.0");
            fixture.AssertFullSemver("1.4.0");

            /****************************************************
            *            END Hotfix branch section              *
            ****************************************************/
        }
    }
}