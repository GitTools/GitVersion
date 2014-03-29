namespace GitVersion
{
    public class GitFlowVersionFinder
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            if (context.CurrentBranch.IsMaster())
            {
                return new MasterVersionFinder().FindVersion(context.Repository, context.CurrentBranch.Tip);
            }

            if (context.CurrentBranch.IsHotfix())
            {
                return new HotfixVersionFinder().FindVersion(context);
            }

            if (context.CurrentBranch.IsRelease())
            {
                return new ReleaseVersionFinder().FindVersion(context);
            }

            if (context.CurrentBranch.IsDevelop())
            {
                return new DevelopVersionFinder().FindVersion(context);
            }

            if (context.CurrentBranch.IsPullRequest())
            {
                return new PullVersionFinder().FindVersion(context);
            }

            return new FeatureVersionFinder().FindVersion(context);
        }
    }
}