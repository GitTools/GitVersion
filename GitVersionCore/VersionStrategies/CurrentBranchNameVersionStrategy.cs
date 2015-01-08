namespace GitVersion.VersionStrategies
{
    using System.Linq;

    using LibGit2Sharp;

    public class CurrentBranchNameVersionStrategy : VersionStrategyBase
    {
        public override SemanticVersion CalculateVersion(GitVersionContext context)
        {
            
            var versionString = GetSuffix(context, context.CurrentBranch);
            SemanticVersion shortVersion;
            if (SemanticVersion.TryParse(versionString, context.Configuration.TagPrefix, out shortVersion))
            {
                var nbHotfixCommits = BranchCommitDifferenceFinder.NumberOfCommitsInBranchNotKnownFromBaseBranch(context.Repository, context.CurrentBranch, context.CurrentBranchConfig.ReferenceBranch);
                return new SemanticVersion
                           {
                               Major = shortVersion.Major,
                               Minor = shortVersion.Minor,
                               Patch = shortVersion.Patch,
                               PreReleaseTag = new SemanticVersionPreReleaseTag(context.CurrentBranchConfig.Tag, 1),
                               BuildMetaData = new SemanticVersionBuildMetaData(nbHotfixCommits, context.CurrentBranch.Name, context.CurrentCommit.Sha, context.CurrentCommit.When())
                           };
            }
            return null;
        }

        static string GetSuffix(GitVersionContext context, Branch branch)
        {
            return context.CurrentBranchConfig.Prefixes.Aggregate(branch.Name, (seed, prefix) => ExtensionMethods.TrimStart(seed, prefix));
        }
    }
}