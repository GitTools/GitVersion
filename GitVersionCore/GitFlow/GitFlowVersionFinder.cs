namespace GitVersion
{
    public class GitFlowVersionFinder
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            if (context.CurrentBranch.IsMaster())
            {
                return new MasterVersionFinder().FindVersion(context);
            }

            if (context.CurrentBranch.IsHotfix())
            {
                context.CurrentBranchConfig = context.Configuration.Release;
                return new HotfixVersionFinder().FindVersion(context);
            }

            if (context.CurrentBranch.IsRelease())
            {
                context.CurrentBranchConfig = context.Configuration.Release;
                return new ReleaseVersionFinder().FindVersion(context);
            }

            if (context.CurrentBranch.IsDevelop())
            {
                context.CurrentBranchConfig = context.Configuration.Develop;
                return new DevelopVersionFinder().FindVersion(context);
            }

            if (context.CurrentBranch.IsPullRequest())
            {
                return new PullVersionFinder().FindVersion(context);
            }

            if (context.CurrentBranch.IsSupport())
            {
                return new SupportVersionFinder().FindVersion(context.Repository, context.CurrentCommit, context.Configuration);
            }

            return new FeatureVersionFinder().FindVersion(context);
        }
    }
}