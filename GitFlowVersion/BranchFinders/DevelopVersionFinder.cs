namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    public class DevelopVersionFinder
    {
        public Commit Commit { get; set; }
        public Repository Repository { get; set; }
        
        public SemanticVersion FindVersion()
        {
            var masterBranch = Repository.Branches.First(b => b.Name == "master");
            var developBranch = Repository.Branches.First(b => b.Name == "develop");

            var firstCommitOnMasterOlderThanDevelopCommitThatIsAMergeCommit = masterBranch.Commits.SkipWhile(c => c.Committer.When > Commit.Committer.When).First(c => c.Message.StartsWith("merge"));


            var versionString = MasterVersionFinder.GetVersionFromMergeCommit(
                    firstCommitOnMasterOlderThanDevelopCommitThatIsAMergeCommit.Message);


            var version = SemanticVersion.FromMajorMinorPatch(versionString);

            version.Minor++;
            version.Patch = 0;

            version.Stage = Stage.Unstable;


            version.PreRelease = developBranch.Commits
                .SkipWhile(x => x != Commit)
                .TakeWhile(x =>x.Committer.When >= firstCommitOnMasterOlderThanDevelopCommitThatIsAMergeCommit.Committer.When)
                .Count();

            return version;
        }
    }
}