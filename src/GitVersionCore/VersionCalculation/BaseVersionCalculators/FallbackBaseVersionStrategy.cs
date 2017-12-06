namespace GitVersion.VersionCalculation
{
    using System.Collections.Generic;
    using System.Linq;
    using BaseVersionCalculators;
    using LibGit2Sharp;

    /// <summary>
    /// Version is 0.1.0.
    /// BaseVersionSource is the "root" commit reachable from the current commit.
    /// Does not increment.
    /// </summary>
    public class FallbackBaseVersionStrategy : BaseVersionStrategy
    {
        public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            var source = new BaseVersionSource(
                context.RepositoryMetadata.CurrentBranch.Root,
                $"Fallback to root commit of {context.RepositoryMetadata.CurrentBranch.Name}");
            yield return new BaseVersion(context, false, new SemanticVersion(minor: 1), source, null);
        }
    }
}