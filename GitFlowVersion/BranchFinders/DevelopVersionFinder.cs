namespace GitFlowVersion
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    class DevelopVersionFinder
    {
        public Commit Commit { get; set; }
        public Repository Repository { get; set; }
        
        public SemanticVersion FindVersion()
        {
            var version = GetSemanticVersion();

            version.Minor++;
            version.Patch = 0;

            version.Stage = Stage.Unstable;

            return version;
        }

        SemanticVersion GetSemanticVersion()
        {
            var developBranch = Repository.Branches.First(b => b.Name == "develop");

            var masterBranch = Repository.Branches.First(b => b.Name == "master");
            var firstCommitOnMasterOlderThanDevelopCommitThatIsAMergeCommit = masterBranch.Commits
                                                                                          .SkipWhile(c => c.When() > Commit.When())
                                                                                          .FirstOrDefault(c => c.Message.StartsWith("merge"));

            if (firstCommitOnMasterOlderThanDevelopCommitThatIsAMergeCommit != null)
            {
                var versionString = MergeMessageParser.GetVersionFromMergeCommit(
                    firstCommitOnMasterOlderThanDevelopCommitThatIsAMergeCommit.Message);
                var version = SemanticVersion.FromMajorMinorPatch(versionString);

                version.PreRelease = developBranch.Commits
                                                  .SkipWhile(x => x != Commit)
                                                  .TakeWhile(x => x.When() >= firstCommitOnMasterOlderThanDevelopCommitThatIsAMergeCommit.When())
                                                  .Count();
                return version;
            }
            var firstCommitOnMasterOlderThanDevelopCommitThatIsATagCommit = masterBranch.Commits
                                                                                          .SkipWhile(c => c.When() > Commit.When())
                                                                                          .FirstOrDefault(x => x.SemVerTags().Any());

            if (firstCommitOnMasterOlderThanDevelopCommitThatIsATagCommit != null)
            {
                var versionString = firstCommitOnMasterOlderThanDevelopCommitThatIsATagCommit
                    .SemVerTags()
                    .First()
                    .Name;
                var version = SemanticVersion.FromMajorMinorPatch(versionString);
                version.PreRelease = developBranch.Commits
                                                  .SkipWhile(x => x != Commit)
                                                  //TODO: why is a skip one needed here but not above?
                                                  .Skip(1)
                                                  .TakeWhile(x => x.When() >= firstCommitOnMasterOlderThanDevelopCommitThatIsATagCommit.When())
                                                  .Count();
                return version;
            }

            throw new Exception("Could not find a merge or tag commit on master that is older than the target commit on develop. ");
        }
    }
}