namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    public class LastTagBaseVersionStrategy : BaseVersionStrategy
    {
        public override BaseVersion GetVersion(GitVersionContext context)
        {
            VersionTaggedCommit version;
            if (new LastTaggedReleaseFinder(context).GetVersion(out version))
            {
                var shouldIncrement = version.Commit != context.CurrentCommit;
                return new BaseVersion(shouldIncrement, version.SemVer, version.Commit);
            }

            return null;
        }
    }
}