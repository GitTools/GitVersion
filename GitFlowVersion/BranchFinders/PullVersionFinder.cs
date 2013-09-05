namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    class PullVersionFinder
    {
        public Commit Commit;
        public Repository Repository;
        public Branch PullBranch;

        public SemanticVersion FindVersion()
        {

            var masterBranch = Repository.GetMasterBranch();

            var firstCommitOnMasterOlderThanDevelopCommitThatIsAMergeCommit = masterBranch.Commits.SkipWhile(c => c.When() > Commit.When())
                                                                                          .First(c => c.Message.StartsWith("merge"));


            var versionString = MergeMessageParser.GetVersionFromMergeCommit(
                firstCommitOnMasterOlderThanDevelopCommitThatIsAMergeCommit.Message);


            var version = SemanticVersion.FromMajorMinorPatch(versionString);

            version.Minor++;
            version.Patch = 0;

            version.Stage = Stage.Pull;


            version.Suffix = PullBranch.CanonicalName.Substring(PullBranch.CanonicalName.IndexOf("/pull/") + 6);
            version.PreRelease = 0;


            return version;
        }
    }
}