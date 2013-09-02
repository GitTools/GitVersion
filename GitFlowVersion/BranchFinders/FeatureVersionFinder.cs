namespace GitFlowVersion
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    public class FeatureVersionFinder
    {
        public Commit Commit { get; set; }
        public Repository Repository { get; set; }
        public Branch FeatureBranch { get; set; }

        public SemanticVersion FindVersion()
        {

            var firstCommitOnBranch = Repository.Refs.Log(FeatureBranch.CanonicalName).Last();

            if (firstCommitOnBranch.To == Commit.Id) //no commits on branch
                return new DevelopVersionFinder {Commit = Commit, Repository = Repository}.FindVersion();
            
            var masterBranch = Repository.Branches.First(b => b.Name == "master");
            var developBranch = Repository.Branches.First(b => b.Name == "develop");

            var firstCommitOnMasterOlderThanDevelopCommitThatIsAMergeCommit = masterBranch.Commits.SkipWhile(c => c.Committer.When > Commit.Committer.When)
                .First(c => c.Message.StartsWith("merge"));


            var versionString = MasterVersionFinder.GetVersionFromMergeCommit(
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