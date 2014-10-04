namespace GitVersion
{
    using LibGit2Sharp;

    class SupportVersionFinder
    {
        public SemanticVersion FindVersion(IRepository repository, Commit tip)
        {
            foreach (var tag in repository.TagsByDate(tip))
            {
            ShortVersion shortVersion;
            if (ShortVersionParser.TryParse(tag.Name, out shortVersion))
                {
                    return BuildVersion(tip, shortVersion);
                }
            }

            var semanticVersion = new SemanticVersion();

            ShortVersion versionFromTip;
            if (MergeMessageParser.TryParse(tip, out versionFromTip))
            {
                semanticVersion = BuildVersion(tip, versionFromTip);
            }

            semanticVersion.OverrideVersionManuallyIfNeeded(repository);

            if (semanticVersion == null || semanticVersion.IsEmpty())
            {
                throw new WarningException("The head of a support branch should always be a merge commit if you follow gitflow. Please create one or work around this by tagging the commit with SemVer compatible Id.");
            }

            return semanticVersion;
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