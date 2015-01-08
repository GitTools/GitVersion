namespace GitVersion
{
    using System.Linq;

    using GitVersion.VersionStrategies;

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
                var versionStrategies = new VersionStrategyBase[] { new LastTagVersionStrategy(), new CurrentBranchNameVersionStrategy() };
                return versionStrategies.Select(s => s.CalculateVersion(context)).Max();
            }

            if (context.CurrentBranch.IsRelease())
            {
                var versionStrategies = new VersionStrategyBase[] { new LastTagVersionStrategy(), new CurrentBranchNameVersionStrategy() };
                return versionStrategies.Select(s => s.CalculateVersion(context)).Max();
            }

            if (context.CurrentBranch.IsDevelop())
            {
                var versionStrategies = new VersionStrategyBase[] { new LastTagVersionStrategy(), new CurrentBranchNameVersionStrategy() };
                return versionStrategies.Select(s => s.CalculateVersion(context)).Max();
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