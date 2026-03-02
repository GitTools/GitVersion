using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

/// <summary>
/// Version is extracted from older merge commits.
/// BaseVersionSource is the commit where the message was found.
/// </summary>
internal sealed class MergeCommitVersionStrategy(ILog log, Lazy<GitVersionContext> contextLazy,
    IRepositoryStore repositoryStore, IIncrementStrategyFinder incrementStrategyFinder,     IEffectiveBranchConfigurationFinder effectiveBranchConfigurationFinder,
    ITaggedSemanticVersionRepository taggedSemanticVersionRepository
    )
    : IVersionStrategy
{
    private readonly ILog log = log.NotNull();
    private readonly Lazy<GitVersionContext> contextLazy = contextLazy.NotNull();
    private readonly IRepositoryStore repositoryStore = repositoryStore.NotNull();
    private readonly IIncrementStrategyFinder incrementStrategyFinder = incrementStrategyFinder.NotNull();
    private readonly IEffectiveBranchConfigurationFinder effectiveBranchConfigurationFinder = effectiveBranchConfigurationFinder.NotNull();
    private readonly ITaggedSemanticVersionRepository taggedSemanticVersionRepository = taggedSemanticVersionRepository.NotNull();

    private GitVersionContext Context => contextLazy.Value;

    public IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
    {
        configuration.NotNull();

        if (!Context.Configuration.VersionStrategy.HasFlag(VersionStrategies.MergeCommit))
        {
            yield break;
        }

        var taggedCommits = this.taggedSemanticVersionRepository.GetTaggedSemanticVersions(
            tagPrefix: Context.Configuration.TagPrefixPattern,
            format: Context.Configuration.SemanticVersionFormat,
            ignore: Context.Configuration.Ignore
        );
        var previousVersion = new SemanticVersion(0, 0, 0);

        // Must Loop In Reverse To Ensure Correct Version Calculation
        foreach (var commit in configuration.Value.Ignore.Filter(Context.CurrentBranchCommits.ToArray()).Reverse())
        {
            if (taggedCommits.Contains(commit))
            {
                this.log.Debug($"Found tagged commit {commit}, adjusting previous version.");
                previousVersion = taggedCommits[commit].First().Value;
            }
            if (!commit.IsMergeCommit())
            {
                continue;
            }

            // Using Merge Message Since The Formats Are Identical
            if (!MergeMessage.TryParse(commit, Context.Configuration, out var mergeMessage))
            {
                continue;
            }

            this.log.Info($"Found commit [{commit}] matching merge message format: {mergeMessage.FormatName}");

            var currentBranch = this.repositoryStore.GetTargetBranch(mergeMessage.MergedBranch!.Friendly);
            var branchConfiguration = this.effectiveBranchConfigurationFinder.GetConfigurations(currentBranch, Context.Configuration).First();
            var baseVersionSource = this.repositoryStore.FindMergeBase(commit.Parents[0], commit.Parents[1]);

            var label = branchConfiguration.Value.GetBranchSpecificLabel(mergeMessage.MergedBranch!.Friendly, "");
            var increment = this.incrementStrategyFinder.DetermineIncrementedField(
                    currentCommit: commit,
                    baseVersionSource: baseVersionSource,
                    shouldIncrement: true,
                    configuration: branchConfiguration.Value,
                    label: label
                );

            yield return new BaseVersion($"Merge Commit From '{mergeMessage.MergedBranch?.Friendly}'", previousVersion
            )
            {
                Operator = new()
                {
                    Increment = increment,
                    ForceIncrement = false,
                    Label = label
                }
            };

            previousVersion = previousVersion.Increment(
                increment,
                label,
                true
            );
        }
    }
}
