namespace GitVersion
{
    using LibGit2Sharp;

    class MasterVersionFinder
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

            ShortVersion versionFromTip;
            if (MergeMessageParser.TryParse(tip, out versionFromTip))
            {
                semanticVersion = BuildVersion(repository, tip, versionFromTip);
            }

            if (semanticVersion == null || semanticVersion.IsEmpty())
            {
                throw new WarningException("The head of master should always be a merge commit if you follow gitflow. Please create one or work around this by tagging the commit with SemVer compatible Id.");
            }

            return semanticVersion;
        }

        SemanticVersion BuildVersion(IRepository repository, Commit tip, ShortVersion shortVersion)
        {
            var releaseDate = ReleaseDateFinder.Execute(repository, tip, shortVersion.Patch);
            return new SemanticVersion
            {
                Major = shortVersion.Major,
                Minor = shortVersion.Minor,
                Patch = shortVersion.Patch,
                BuildMetaData = new SemanticVersionBuildMetaData(null, "master", releaseDate)
            };
        }
    }
}