namespace GitVersion
{
    using System.Linq;
    using LibGit2Sharp;

    class DevelopVersionFinder
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            var versionOnMasterFinder = new VersionOnMasterFinder();
            var tip = context.CurrentCommit;
            var versionFromMaster = versionOnMasterFinder.Execute(context, tip.When());

            var f = new CommitFilter
            {
                Since = tip,
                Until = context.Repository.FindBranch("master").Tip,
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time
            };

            var c = context.Repository.Commits.QueryBy(f);
            var numberOfCommitsSinceRelease = c.Count();

            var semanticVersion = new SemanticVersion
            {
                Major = versionFromMaster.Major,
                Minor = versionFromMaster.Minor + 1,
                Patch = 0,
                PreReleaseTag = context.Configuration.DevelopBranchTag + numberOfCommitsSinceRelease,
                BuildMetaData = new SemanticVersionBuildMetaData(numberOfCommitsSinceRelease, context.CurrentBranch.Name,tip.Sha,tip.When()),
            };

            semanticVersion.OverrideVersionManuallyIfNeeded(context.Repository);

            return semanticVersion;
        }
    }
}