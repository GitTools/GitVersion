namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    class PullVersionFinder
    {
        public Commit Commit { get; set; }
        public Repository Repository { get; set; }
        public Branch PullBranch { get; set; }

        public SemanticVersion FindVersion()
        {

            var masterBranch = Repository.Branches.First(b => b.Name == "master");

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