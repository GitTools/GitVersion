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
internal sealed class MergeMessageVersionStrategy(ILog log, Lazy<GitVersionContext> contextLazy,
    IRepositoryStore repositoryStore, IIncrementStrategyFinder incrementStrategyFinder)
    : IVersionStrategy
{
    private readonly ILog log = log.NotNull();
    private readonly Lazy<GitVersionContext> contextLazy = contextLazy.NotNull();
    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly IIncrementStrategyFinder incrementStrategyFinder = incrementStrategyFinder.NotNull();

    private GitVersionContext Context => contextLazy.Value;

    public IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
        => GetBaseVersionsInternal(configuration).Take(5);

    private IEnumerable<BaseVersion> GetBaseVersionsInternal(EffectiveBranchConfiguration configuration)
    {
        configuration.NotNull();

        if (!Context.Configuration.VersionStrategy.HasFlag(VersionStrategies.MergeMessage)
                || !configuration.Value.TrackMergeMessage)
            yield break;

        foreach (var commit in configuration.Value.Ignore.Filter(Context.CurrentBranchCommits))
        {
            if (MergeMessage.TryParse(commit, Context.Configuration, out var mergeMessage)
                && mergeMessage.Version is not null
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

                var label = configuration.Value.GetBranchSpecificLabel(Context.CurrentBranch.Name, null);
                var increment = shouldIncrement ? incrementStrategyFinder.DetermineIncrementedField(
                    currentCommit: Context.CurrentCommit,
                    baseVersionSource: baseVersionSource,
                    shouldIncrement: true,
                    configuration: configuration.Value,
                    label: label
                ) : VersionField.None;

                yield return new BaseVersion($"Merge message '{message}'", mergeMessage.Version, baseVersionSource)
                {
                    Operator = new BaseVersionOperator()
                    {
                        Increment = increment,
                        ForceIncrement = false,
                        Label = label
                    }
                };
            }
        }
    }
}
