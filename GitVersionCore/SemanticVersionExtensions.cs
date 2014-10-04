namespace GitVersion
{
    using LibGit2Sharp;

    public static class SemanticVersionExtensions
    {
        public static void OverrideVersionManuallyIfNeeded(this SemanticVersion version, IRepository repository)
        {
            var nextVersionTxtFileFinder = new NextVersionTxtFileFinder(repository.GetRepositoryDirectory());
            SemanticVersion manualNextVersion ;
            if (nextVersionTxtFileFinder.TryGetNextVersion(out manualNextVersion))
            {
                if (manualNextVersion > version)
                {
                    version.Major = manualNextVersion.Major;
                    version.Minor = manualNextVersion.Minor;
                    version.Patch = manualNextVersion.Patch;
                }
            }
        }
    }
}