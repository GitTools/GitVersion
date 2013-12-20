namespace GitFlowVersion
{
    class MasterVersionFinder
    {
        public VersionAndBranch FindVersion(GitFlowVersionContext context)
        {
            int major;
            int minor;
            int patch;
            foreach (var tag in context.Repository.TagsByDate(context.Tip))
            {
                if (ShortVersionParser.TryParse(tag.Name, out major, out minor, out patch))
                {
                    return BuildVersion(context, major, minor, patch);
                }
            }

            string versionString;
            if (MergeMessageParser.TryParse(context.Tip, out versionString))
            {
                if (ShortVersionParser.TryParse(versionString, out major, out minor, out patch))
                {
                    return BuildVersion(context, major, minor, patch);
                }
            }

            throw new ErrorException("The head of master should always be a merge commit if you follow gitflow. Please create one or work around this by tagging the commit with SemVer compatible Id.");
        }

        VersionAndBranch BuildVersion(GitFlowVersionContext context, int major, int minor, int patch)
        {
            return new VersionAndBranch
            {
                BranchType = BranchType.Master,
                BranchName = "master",
                Sha = context.Tip.Sha,
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