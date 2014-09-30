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
                    return BuildVersion(repository, tip, shortVersion);
                }
            }

            var semanticVersion = new SemanticVersion();

            string versionString;
            if (MergeMessageParser.TryParse(tip, out versionString))
            {
                ShortVersion shortVersion;
                if (ShortVersionParser.TryParse(versionString, out shortVersion))
                {
                    semanticVersion = BuildVersion(repository, tip, shortVersion);
                }
            }

            semanticVersion.OverrideVersionManuallyIfNeeded(repository);

            if (semanticVersion == null || semanticVersion.IsEmpty())
            {
                throw new WarningException("The head of a support branch should always be a merge commit if you follow gitflow. Please create one or work around this by tagging the commit with SemVer compatible Id.");
            }

            return semanticVersion;
        }

        SemanticVersion BuildVersion(IRepository repository, Commit tip, ShortVersion shortVersion)
        {
            var releaseDate = ReleaseDateFinder.Execute(repository, tip.Sha, shortVersion.Patch);
            return new SemanticVersion
            {
                Major = shortVersion.Major,
                Minor = shortVersion.Minor,
                Patch = shortVersion.Patch,
                BuildMetaData = new SemanticVersionBuildMetaData(null, "support", releaseDate)
            };
        } 
    }
}