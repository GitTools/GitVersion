namespace GitVersion
{
    using LibGit2Sharp;

    class MasterVersionFinder
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            foreach (var tag in context.Repository.TagsByDate(context.CurrentCommit))
            {
                SemanticVersion shortVersion;
                if (SemanticVersion.TryParse(tag.Name, context.Configuration.GitTagPrefix, out shortVersion))
                {
                    return BuildVersion(context.CurrentCommit, shortVersion);
                }
            }

            SemanticVersion versionFromTip;
            if (MergeMessageParser.TryParse(context.CurrentCommit, context.Configuration, out versionFromTip))
            {
                return BuildVersion(context.CurrentCommit, versionFromTip);
            }
            throw new WarningException("The head of master should always be a merge commit if you follow gitflow. Please create one or work around this by tagging the commit with SemVer compatible Id.");
        }

        SemanticVersion BuildVersion(Commit tip, SemanticVersion shortVersion)
        {
            return new SemanticVersion
            {
                Major = shortVersion.Major,
                Minor = shortVersion.Minor,
                Patch = shortVersion.Patch,
                BuildMetaData = new SemanticVersionBuildMetaData(null, "master",tip.Sha,tip.When())
            };
        }
    }
}