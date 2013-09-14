namespace GitFlowVersion
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    class MasterVersionFinder
    {
        public Commit Commit;

        public VersionInformation FindVersion()
        {
            var versionString = GetVersionString();

            var version = VersionInformation.FromMajorMinorPatch(versionString);

            version.BranchType = BranchType.Master;
            version.BranchName = "master";
            version.Sha = Commit.Sha;
            return version;
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