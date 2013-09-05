namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    class FeatureVersionFinder
    {
        public Commit Commit;
        public Repository Repository;
        public Branch FeatureBranch;

        public SemanticVersion FindVersion()
        {

            var firstCommitOnBranch = Repository.Refs.Log(FeatureBranch.CanonicalName).Last();

            if (firstCommitOnBranch.To == Commit.Id) //no commits on branch
            {
                return new DevelopVersionFinder
                       {
                           Commit = Commit, 
                           Repository = Repository
                       }
                    .FindVersion();
            }

            var masterBranch = Repository.GetMasterBranch();

            var firstCommitOnMasterOlderThanDevelopCommitThatIsAMergeCommit = masterBranch.Commits
                .SkipWhile(c => c.When() > Commit.When())
                .First(c => c.Message.StartsWith("merge"));


            var versionString = MergeMessageParser.GetVersionFromMergeCommit(
                    firstCommitOnMasterOlderThanDevelopCommitThatIsAMergeCommit.Message);


            var version = SemanticVersion.FromMajorMinorPatch(versionString);

            version.Minor++;
            version.Patch = 0;

            version.Stage = Stage.Feature;
            
            version.Suffix = firstCommitOnBranch.To.Sha.Substring(0,8);
            version.PreRelease = 0;
            
            return version;
        }
    }
}