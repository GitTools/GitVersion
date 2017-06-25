namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System.Collections.Generic;
    using System.Linq;
    using LibGit2Sharp;

    /// <summary>
    /// Version is extracted from older commits's merge messages.
    /// BaseVersionSource is the commit where the message was found.
    /// Increments if PreventIncrementForMergedBranchVersion (from the branch config) is false.
    /// </summary>
    public class MergeMessageBaseVersionStrategy : BaseVersionStrategy
    {
        public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            return context
                .RepositoryMetadata
                .CurrentBranch
                .MergeMessages
                .Where(m => m.Version != null)
                .Select(m =>
                {
                    var shouldIncrement = !context.Configuration.PreventIncrementForMergedBranchVersion;
                    var source = new BaseVersionSource(m.SourceCommit, $"Merge message '{m.SourceCommit.Message.Trim()}' @ {m.SourceCommit.Sha}");
                    return new BaseVersion(context, shouldIncrement, m.Version, source, null);
                });
        }
    }
}