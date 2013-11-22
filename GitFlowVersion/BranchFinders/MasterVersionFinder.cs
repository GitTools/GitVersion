namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    class MasterVersionFinder
    {
        public Commit Commit;
        public IRepository Repository;

        public VersionAndBranch FindVersion()
        {
            int major;
            int minor;
            int patch;
            foreach (var tag in Repository.Tags.Where(tag => tag.Target == Commit).Reverse())
            {
                if (ShortVersionParser.TryParse(tag.Name, out major, out minor, out patch))
                {
                    return BuildVersion(major, minor, patch);
                }
            }

            string versionString;
            if (MergeMessageParser.TryParse(Commit, out versionString))
            {
                if (ShortVersionParser.TryParse(versionString, out major, out minor, out patch))
                {
                    return BuildVersion(major, minor, patch);
                }
            }

            throw new ErrorException("The head of master should always be a merge commit if you follow gitflow. Please create one or work around this by tagging the commit with SemVer compatible Id.");
        }

        VersionAndBranch BuildVersion(int major, int minor, int patch)
        {
            return new VersionAndBranch
            {
                BranchType = BranchType.Master,
                BranchName = "master",
                Sha = Commit.Sha,
                Version = new SemanticVersion
                {
                    Major = major,
                    Minor = minor,
                    Patch = patch,
                    Stability = Stability.Final
                }
            };
        }
    }
}