namespace GitVersion
{
    using LibGit2Sharp;

    class SupportVersionFinder
    {
        public SemanticVersion FindVersion(IRepository repository, Commit tip)
        {
            int major;
            int minor;
            int patch;
            foreach (var tag in repository.TagsByDate(tip))
            {
                if (ShortVersionParser.TryParse(tag.Name, out major, out minor, out patch))
                {
                    return BuildVersion(repository, tip, major, minor, patch);
                }
            }

            var semanticVersion = new SemanticVersion();

            string versionString;
            if (MergeMessageParser.TryParse(tip, out versionString))
            {
                if (ShortVersionParser.TryParse(versionString, out major, out minor, out patch))
                {
                    semanticVersion = BuildVersion(repository, tip, major, minor, patch);
                }
            }

            semanticVersion.OverrideVersionManuallyIfNeeded(repository);

            if (semanticVersion == null || semanticVersion.IsEmpty())
            {
                throw new WarningException("The head of a support branch should always be a merge commit if you follow gitflow. Please create one or work around this by tagging the commit with SemVer compatible Id.");
            }

            return semanticVersion;
        }

        SemanticVersion BuildVersion(IRepository repository, Commit tip, int major, int minor, int patch)
        {
            var releaseDate = ReleaseDateFinder.Execute(repository, tip.Sha, patch);
            return new SemanticVersion
            {
                Major = major,
                Minor = minor,
                Patch = patch,
                BuildMetaData = new SemanticVersionBuildMetaData(null, "support", releaseDate)
            };
        } 
    }
}