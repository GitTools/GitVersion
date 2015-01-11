namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    public class LastTagBaseVersionStrategy : BaseVersionStrategy
    {
        public override BaseVersion GetVersion(GitVersionContext context)
        {
            VersionTaggedCommit version;
            if (new LastTaggedReleaseFinder(context).GetVersion(out version))
            {
                var shouldUpdateVersion = version.Commit != context.CurrentCommit;
                return new BaseVersion(shouldUpdateVersion, shouldUpdateVersion, version.SemVer, version.Commit);
            }

            return null;
        }
    }
}