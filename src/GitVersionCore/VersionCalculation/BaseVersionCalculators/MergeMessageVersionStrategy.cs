using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation
{
    /// <summary>
    /// Version is extracted from older commits's merge messages.
    /// BaseVersionSource is the commit where the message was found.
    /// Increments if PreventIncrementForMergedBranchVersion (from the branch config) is false.
    /// </summary>
    public class MergeMessageVersionStrategy : IVersionStrategy
    {
        private readonly ILog log;

        public MergeMessageVersionStrategy(ILog log)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public virtual IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            var commitsPriorToThan = context.CurrentBranch
                .CommitsPriorToThan(context.CurrentCommit.When());
            var baseVersions = commitsPriorToThan
                .SelectMany(c =>
                {
                    if (TryParse(c, context, out var mergeMessage) &&
                        mergeMessage.Version != null &&
                        context.FullConfiguration.IsReleaseBranch(TrimRemote(mergeMessage.MergedBranch)))
                    {
                        log.Info($"Found commit [{context.CurrentCommit.Sha}] matching merge message format: {mergeMessage.FormatName}");
                        var shouldIncrement = !context.Configuration.PreventIncrementForMergedBranchVersion;
                        return new[]
                        {
                            new BaseVersion(context, $"{MergeMessageStrategyPrefix} '{c.Message.Trim()}'", shouldIncrement, mergeMessage.Version, c, null)
                        };
                    }
                    return Enumerable.Empty<BaseVersion>();
                })
                .Take(2)
                .ToList();
            return baseVersions;
        }

        public static readonly string MergeMessageStrategyPrefix = "Merge message";

        private static bool TryParse(Commit mergeCommit, GitVersionContext context, out MergeMessage mergeMessage)
        {
            mergeMessage = Inner(mergeCommit, context);
            return mergeMessage != null;
        }

        private static MergeMessage Inner(Commit mergeCommit, GitVersionContext context)
        {
            if (mergeCommit.Parents.Count() < 2)
            {
                return null;
            }

            var mergeMessage = new MergeMessage(mergeCommit.Message, context.FullConfiguration);
            return mergeMessage;
        }

        private static string TrimRemote(string branchName) => branchName
            .RegexReplace("^refs/remotes/", string.Empty, RegexOptions.IgnoreCase)
            .RegexReplace("^origin/", string.Empty, RegexOptions.IgnoreCase);
    }
}
