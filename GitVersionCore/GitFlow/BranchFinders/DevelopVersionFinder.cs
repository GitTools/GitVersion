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

            var shortVersion = new SemanticVersion()
                                   {
                                       Major = versionFromMaster.Major,
                                       Minor = versionFromMaster.Minor + 1,
                                       Patch = 0,
                                   };

            var applicableTagsInDescendingOrder = context.Repository.SemVerTagsRelatedToVersion(context.Configuration, shortVersion).OrderByDescending(tag => SemanticVersion.Parse(tag.Name, context.Configuration.TagPrefix)).ToList();
            var preReleaseTag = context.CurrentBranchConfig.VersioningMode.GetInstance().GetPreReleaseTag(context, applicableTagsInDescendingOrder, numberOfCommitsSinceRelease);


            var semanticVersion = new SemanticVersion
            {
                Major = shortVersion.Major,
                Minor = shortVersion.Minor,
                Patch = shortVersion.Patch,
                PreReleaseTag = preReleaseTag,
                BuildMetaData = new SemanticVersionBuildMetaData(numberOfCommitsSinceRelease, context.CurrentBranch.Name,tip.Sha,tip.When()),
            };

            semanticVersion.OverrideVersionManuallyIfNeeded(context.Repository, context.Configuration);

            return semanticVersion;
        }
    }
}