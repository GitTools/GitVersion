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
internal class MergeMessageVersionStrategy : VersionStrategyBase
{
    private readonly ILog log;
    private readonly IRepositoryStore repositoryStore;

    public MergeMessageVersionStrategy(ILog log, Lazy<GitVersionContext> versionContext, IRepositoryStore repositoryStore) : base(versionContext)
    {
        this.log = log.NotNull();
        this.repositoryStore = repositoryStore.NotNull();
    }

    public override IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
    {
        if (!configuration.Value.TrackMergeMessage || Context.Configuration.VersioningMode == VersioningMode.TrunkBased)
            return Enumerable.Empty<BaseVersion>();

        var commitsPriorToThan = Context.CurrentBranch.Commits.GetCommitsPriorTo(Context.CurrentCommit.When);
        var baseVersions = commitsPriorToThan
            .SelectMany(commit =>
            {
                if (TryParse(commit, Context, out var mergeMessage) && mergeMessage.Version != null
                    && Context.Configuration.IsReleaseBranch(mergeMessage.MergedBranch!))
                {
                    this.log.Info($"Found commit [{commit}] matching merge message format: {mergeMessage.FormatName}");
                    var shouldIncrement = !configuration.Value.PreventIncrementOfMergedBranchVersion;

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
                return Enumerable.Empty<BaseVersion>();
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
