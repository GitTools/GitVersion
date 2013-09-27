namespace GitFlowVersion
{
    using System;
    using LibGit2Sharp;

    class MasterVersionFinder
    {
        public Commit Commit;
        public IRepository Repository;

        public VersionAndBranch FindVersion()
        {
            var version = GetVersion();
            if (version.Stability == null)
            {
                version.Stability = Stability.Final;
            }
            return new VersionAndBranch
            {
                BranchType = BranchType.Master,
                BranchName = "master",
                Sha = Commit.Sha,
                Version = version
            };
        }

        SemanticVersion GetVersion()
        {
            //TODO: should we take the newest or the highest? perhaps it doesnt matter?
            var versionTag = Repository
                .SemVerTag(Commit);

            if (versionTag != null)
            {
                return versionTag;
            }

            string versionString;
            if (MergeMessageParser.TryParse(Commit.Message, out versionString))
            {
                SemanticVersion version;
                if (SemanticVersionParser.TryParse(versionString, out version))
                {
                    return version;
                }
            }
            throw new Exception("The head of master should always be a merge commit if you follow gitflow. Please create one or work around this by tagging the commit with SemVer compatible Id.");
        }
    }
}