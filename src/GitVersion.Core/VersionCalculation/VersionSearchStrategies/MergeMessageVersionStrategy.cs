using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

/// <summary>
/// Version is extracted from older commits' merge messages.
/// BaseVersionSource is the commit where the message was found.
/// Increments if PreventIncrementOfMergedBranchVersion (from the branch configuration) is false.
/// </summary>
internal sealed class MergeMessageVersionStrategy(ILog log, Lazy<GitVersionContext> contextLazy,
    IRepositoryStore repositoryStore, IIncrementStrategyFinder incrementStrategyFinder,
    IEnvironment environment)
    : IVersionStrategy
{
    private readonly ILog log = log.NotNull();
    private readonly Lazy<GitVersionContext> contextLazy = contextLazy.NotNull();
    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly IIncrementStrategyFinder incrementStrategyFinder = incrementStrategyFinder.NotNull();
    private readonly IEnvironment environment = environment.NotNull();

    private GitVersionContext Context => contextLazy.Value;

    public IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
        => GetBaseVersionsInternal(configuration).Take(5);

    private IEnumerable<BaseVersion> GetBaseVersionsInternal(EffectiveBranchConfiguration configuration)
    {
        configuration.NotNull();

        if (!Context.Configuration.VersionStrategy.HasFlag(VersionStrategies.MergeMessage)
                || !configuration.Value.TrackMergeMessage)
        {
            yield break;
        }

        foreach (var commit in configuration.Value.Ignore.Filter(Context.CurrentBranchCommits.ToArray()))
        {
            if (!MergeMessage.TryParse(commit, Context.Configuration, out var mergeMessage)
                || mergeMessage.Version is null
                || !Context.Configuration.IsReleaseBranch(mergeMessage.MergedBranch!))
            {
                continue;
            }

            this.log.Info($"Found commit [{commit}] matching merge message format: {mergeMessage.FormatName}");

            var baseVersionSource = commit;
            if (commit.IsMergeCommit)
            {
                baseVersionSource = this.repositoryStore.FindMergeBase(commit.Parents[0], commit.Parents[1]);
            }

            var label = configuration.Value.GetBranchSpecificLabel(Context.CurrentBranch.Name, null, this.environment);
            var increment = configuration.Value.PreventIncrementOfMergedBranch
                ? VersionField.None : this.incrementStrategyFinder.DetermineIncrementedField(
                    currentCommit: Context.CurrentCommit,
                    baseVersionSource: baseVersionSource,
                    shouldIncrement: true,
                    configuration: configuration.Value,
                    label: label
                );

            yield return new BaseVersion($"Merge message '{commit.Message.Trim()}'", mergeMessage.Version)
            {
                Operator = new()
                {
                    Increment = increment,
                    ForceIncrement = false,
                    Label = label
                }
            };
        }
    }
}
