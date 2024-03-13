using System.Diagnostics.CodeAnalysis;
using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

/// <summary>
/// Version is extracted from older commits' merge messages.
/// BaseVersionSource is the commit where the message was found.
/// Increments if PreventIncrementOfMergedBranchVersion (from the branch configuration) is false.
/// </summary>
internal class MergeMessageVersionStrategy(ILog log, Lazy<GitVersionContext> versionContext, IRepositoryStore repositoryStore)
    : VersionStrategyBase(versionContext)
{
    private readonly ILog log = log.NotNull();
    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();

    public override IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
    {
        if (!Context.Configuration.VersionStrategy.HasFlag(VersionStrategies.MergeMessage) || !configuration.Value.TrackMergeMessage)
            return [];

        var commitsPriorToThan = configuration.Value.Ignore.Filter(Context.CurrentBranchCommits);
        var baseVersions = commitsPriorToThan
            .SelectMany(commit =>
            {
                if (TryParse(commit, Context, out var mergeMessage) && mergeMessage.Version != null
                    && Context.Configuration.IsReleaseBranch(mergeMessage.MergedBranch!))
                {
                    this.log.Info($"Found commit [{commit}] matching merge message format: {mergeMessage.FormatName}");
                    var shouldIncrement = !configuration.Value.PreventIncrementOfMergedBranch;

                    var message = commit.Message.Trim();

                    var baseVersionSource = commit;

                    if (shouldIncrement)
                    {
                        var parents = commit.Parents.ToArray();
                        if (parents.Length == 2 && message.Contains("Merge branch") && message.Contains("release"))
                        {
                            baseVersionSource = this.repositoryStore.FindMergeBase(parents[0], parents[1]);
                        }
                    }

                    var baseVersion = new BaseVersion(
                        source: $"Merge message '{message}'",
                        shouldIncrement: shouldIncrement,
                        semanticVersion: mergeMessage.Version,
                        baseVersionSource: baseVersionSource,
                        branchNameOverride: null
                    );
                    return new[] { baseVersion };
                }
                return [];
            })
            .Take(5)
            .ToList();
        return baseVersions;
    }

    private static bool TryParse(ICommit mergeCommit, GitVersionContext context, [NotNullWhen(true)] out MergeMessage? mergeMessage)
    {
        mergeMessage = Inner(mergeCommit, context);
        return mergeMessage != null;
    }

    private static MergeMessage? Inner(ICommit mergeCommit, GitVersionContext context)
    {
        if (mergeCommit.Parents.Count() < 2)
        {
            return null;
        }

        var mergeMessage = new MergeMessage(mergeCommit.Message, context.Configuration);
        return mergeMessage;
    }
}
