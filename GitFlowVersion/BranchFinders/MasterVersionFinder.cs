namespace GitFlowVersion
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    class MasterVersionFinder
    {
        public Commit Commit;

        public VersionAndBranch FindVersion()
        {
            var versionString = GetVersionString();

            var version = SemanticVersion.FromMajorMinorPatch(versionString);

            return new VersionAndBranch
            {
                BranchType = BranchType.Master,
                BranchName = "master",
                Sha = Commit.Sha,
                Version = version
            };
        }

        string GetVersionString()
        {
            //TODO: should we take the newest or the highest? perhaps it doesnt matter?
            var versionTag = Commit.SemVerTags()
                                   .FirstOrDefault();
            if (versionTag != null)
            {
                return versionTag.Name;
            }

            if (!Commit.Message.StartsWith("merge "))
            {
                throw new Exception("The head of master should always be a merge commit if you follow gitflow. Please create one or work around this by tagging the commit with SemVer compatible Id.");
            }

            return MergeMessageParser.GetVersionFromMergeCommit(Commit.Message);
        }
    }
}