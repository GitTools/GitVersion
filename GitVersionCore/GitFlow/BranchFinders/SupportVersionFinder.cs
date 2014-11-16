namespace GitVersion
{
    using LibGit2Sharp;

    class SupportVersionFinder
    {
        public SemanticVersion FindVersion(IRepository repository, Commit tip, Config configuration)
        {
            foreach (var tag in repository.TagsByDate(tip))
            {
                ShortVersion shortVersion;
                if (ShortVersionParser.TryParse(tag.Name, out shortVersion))
                {
                    return BuildVersion(tip, shortVersion);
                }
            }

            ShortVersion versionFromTip;
            if (MergeMessageParser.TryParse(tip, out versionFromTip))
            {
                var semanticVersion = BuildVersion(tip, versionFromTip);
                semanticVersion.OverrideVersionManuallyIfNeeded(repository, configuration);
                return semanticVersion;
            }
            throw new WarningException("The head of a support branch should always be a merge commit if you follow gitflow. Please create one or work around this by tagging the commit with SemVer compatible Id.");
        }

        SemanticVersion BuildVersion(Commit tip, ShortVersion shortVersion)
        {
            return new SemanticVersion
            {
                Major = shortVersion.Major,
                Minor = shortVersion.Minor,
                Patch = shortVersion.Patch,
                BuildMetaData = new SemanticVersionBuildMetaData(null, "support", tip.Sha,tip.When())
            };
        } 
    }
}