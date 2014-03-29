namespace GitVersion
{
    using System.Linq;

    class DevelopVersionFinder
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            var versionOnMasterFinder = new VersionOnMasterFinder();
            var tip = context.CurrentBranch.Tip;
            var versionFromMaster = versionOnMasterFinder.Execute(context, tip.When());

            var developBranch = context.Repository.FindBranch("develop");
            var numberOfCommitsSinceRelease = developBranch.Commits
                .SkipWhile(x => x != tip)
                .TakeWhile(x => x.When() >= versionFromMaster.Timestamp)
                .Count();

            var releaseDate = ReleaseDateFinder.Execute(context.Repository, tip.Sha, 0);
            var semanticVersion = new SemanticVersion
            {
                Major = versionFromMaster.Major,
                Minor = versionFromMaster.Minor + 1,
                Patch = 0,
                PreReleaseTag = Stability.Unstable.ToString().ToLower() + numberOfCommitsSinceRelease,
                BuildMetaData = new SemanticVersionBuildMetaData(numberOfCommitsSinceRelease, context.CurrentBranch.Name, tip.Sha,
                    releaseDate.OriginalDate, releaseDate.Date),
            };
            return semanticVersion;
        }

    }
}