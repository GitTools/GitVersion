namespace GitVersion
{
    using LibGit2Sharp;

    class MasterVersionFinder
    {
        public VersionAndBranch FindVersion(IRepository repository, Commit tip)
        {
            int major;
            int minor;
            int patch;
            foreach (var tag in repository.TagsByDate(tip))
            {
                if (ShortVersionParser.TryParse(tag.Name, out major, out minor, out patch))
                {
                    return BuildVersion(tip, major, minor, patch);
                }
            }

            string versionString;
            if (MergeMessageParser.TryParse(tip, out versionString))
            {
                if (ShortVersionParser.TryParse(versionString, out major, out minor, out patch))
                {
                    return BuildVersion(tip, major, minor, patch);
                }
            }

            throw new ErrorException("The head of master should always be a merge commit if you follow gitflow. Please create one or work around this by tagging the commit with SemVer compatible Id.");
        }

        VersionAndBranch BuildVersion(Commit tip, int major, int minor, int patch)
        {
            return new VersionAndBranch
            {
                BranchType = BranchType.Master,
                BranchName = "master",
                Sha = tip.Sha,
                Version = new SemanticVersion
                {
                    Major = major,
                    Minor = minor,
                    Patch = patch
                }
            };
        }
    }

}