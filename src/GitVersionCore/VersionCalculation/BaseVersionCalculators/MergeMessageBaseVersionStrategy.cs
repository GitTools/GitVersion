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
            var commitsPriorToThan = context.CurrentBranch
                .CommitsPriorToThan(context.CurrentCommit.When());
            var baseVersions = commitsPriorToThan
                .SelectMany(c =>
                {
                    if (TryParse(c, context, out var mergeMessage) &&
                        mergeMessage.Version != null &&
                        context.FullConfiguration.IsReleaseBranch(mergeMessage.MergedBranch.AsBranchNameWithoutRemote(context)))
                    {
                        var shouldIncrement = !context.Configuration.PreventIncrementForMergedBranchVersion;
                        return new[]
                        {
                            new BaseVersion(context, $"Merge message '{c.Message.Trim()}'", shouldIncrement, mergeMessage.Version, c, null)
                        };
                    }
                    return Enumerable.Empty<BaseVersion>();
                }).ToList();
            return baseVersions;
        }

        static bool TryParse(Commit mergeCommit, GitVersionContext context, out MergeMessage mergeMessage)
        {
            mergeMessage = Inner(mergeCommit, context);
            return mergeMessage != null;
        }

        static MergeMessage Inner(Commit mergeCommit, GitVersionContext context)
        {
            if (mergeCommit.Parents.Count() < 2)
            {
                return null;
            }

            var mergeMessage = new MergeMessage(mergeCommit.Message, context.FullConfiguration);
            return mergeMessage;
        }
    }
}
