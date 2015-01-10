namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    public class LastTagBaseVersionStrategy : BaseVersionStrategy
    {
        public override BaseVersion GetVersion(GitVersionContext context)
        {
            VersionTaggedCommit version;
            if (new LastTaggedReleaseFinder(context).GetVersion(out version))
                return new BaseVersion(true, version.SemVer);

            return null;
        }
    }
}