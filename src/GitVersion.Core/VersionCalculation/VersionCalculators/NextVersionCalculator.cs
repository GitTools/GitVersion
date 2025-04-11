using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using GitVersion.Configuration;
using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.Git;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal class NextVersionCalculator(
    ILog log,
    Lazy<GitVersionContext> versionContext,
    IEnumerable<IDeploymentModeCalculator> deploymentModeCalculators,
    IEnumerable<IVersionStrategy> versionStrategies,
    IEffectiveBranchConfigurationFinder effectiveBranchConfigurationFinder,
    ITaggedSemanticVersionService taggedSemanticVersionService)
    : INextVersionCalculator
{
    private readonly ILog log = log.NotNull();
    private readonly Lazy<GitVersionContext> versionContext = versionContext.NotNull();
    private readonly IVersionStrategy[] versionStrategies = versionStrategies.NotNull().ToArray();
    private readonly IEffectiveBranchConfigurationFinder effectiveBranchConfigurationFinder = effectiveBranchConfigurationFinder.NotNull();

    private GitVersionContext Context => this.versionContext.Value;

    public virtual SemanticVersion FindVersion()
    {
        this.log.Info($"Running against branch: {Context.CurrentBranch} ({Context.CurrentCommit.ToString() ?? "-"})");

        var branchConfiguration = Context.Configuration.GetBranchConfiguration(Context.CurrentBranch);
        EffectiveConfiguration effectiveConfiguration = new(Context.Configuration, branchConfiguration);

        var someBranchRelatedPropertiesMightBeNotKnown = branchConfiguration.Increment == IncrementStrategy.Inherit;

        if (Context.IsCurrentCommitTagged && !someBranchRelatedPropertiesMightBeNotKnown && effectiveConfiguration.PreventIncrementWhenCurrentCommitTagged)
        {
            var allTaggedSemanticVersions = taggedSemanticVersionService.GetTaggedSemanticVersions(
                branch: Context.CurrentBranch,
                configuration: Context.Configuration,
                label: null,
                notOlderThan: Context.CurrentCommit.When,
                taggedSemanticVersion: effectiveConfiguration.GetTaggedSemanticVersion()
            );
            var taggedSemanticVersionsOfCurrentCommit = allTaggedSemanticVersions[Context.CurrentCommit].ToList();

            if (TryGetSemanticVersion(effectiveConfiguration, taggedSemanticVersionsOfCurrentCommit, out var value))
            {
                return value;
            }
        }

        var nextVersion = CalculateNextVersion(Context.CurrentBranch, Context.Configuration);

        if (Context.IsCurrentCommitTagged && someBranchRelatedPropertiesMightBeNotKnown
            && nextVersion.Configuration.PreventIncrementWhenCurrentCommitTagged)
        {
            var allTaggedSemanticVersions = taggedSemanticVersionService.GetTaggedSemanticVersions(
                branch: Context.CurrentBranch,
                configuration: Context.Configuration,
                label: null,
                notOlderThan: Context.CurrentCommit.When,
                taggedSemanticVersion: nextVersion.Configuration.GetTaggedSemanticVersion()
            );
            var taggedSemanticVersionsOfCurrentCommit = allTaggedSemanticVersions[Context.CurrentCommit].ToList();

            if (TryGetSemanticVersion(nextVersion.Configuration, taggedSemanticVersionsOfCurrentCommit, out var value))
            {
                return value;
            }
        }

        var semanticVersion = CalculateSemanticVersion(
            deploymentMode: nextVersion.Configuration.DeploymentMode,
            semanticVersion: nextVersion.IncrementedVersion,
            baseVersionSource: nextVersion.BaseVersion.BaseVersionSource
        );

        var ignore = Context.Configuration.Ignore;
        var alternativeSemanticVersion = taggedSemanticVersionService.GetTaggedSemanticVersionsOfBranch(
            branch: nextVersion.BranchConfiguration.Branch,
            tagPrefix: Context.Configuration.TagPrefixPattern,
            format: Context.Configuration.SemanticVersionFormat,
            ignore: Context.Configuration.Ignore,
            notOlderThan: Context.CurrentCommit.When
        ).Where(element => element.Key.When <= Context.CurrentCommit.When
            && !(element.Key.When <= ignore.Before) && !ignore.Shas.Contains(element.Key.Sha)
        ).SelectMany(element => element).Max()?.Value;

        if (alternativeSemanticVersion is not null
            && semanticVersion.IsLessThan(alternativeSemanticVersion, includePreRelease: false))
        {
            semanticVersion = new SemanticVersion(semanticVersion)
            {
                Major = alternativeSemanticVersion.Major,
                Minor = alternativeSemanticVersion.Minor,
                Patch = alternativeSemanticVersion.Patch
            };
        }

        return semanticVersion;
    }

    private bool TryGetSemanticVersion(
        EffectiveConfiguration effectiveConfiguration,
        IReadOnlyCollection<SemanticVersionWithTag> taggedSemanticVersionsOfCurrentCommit,
        [NotNullWhen(true)] out SemanticVersion? result)
    {
        result = null;

        var label = effectiveConfiguration.GetBranchSpecificLabel(Context.CurrentBranch.Name, null);
        var currentCommitTaggedVersion = taggedSemanticVersionsOfCurrentCommit
            .Where(element => element.Value.IsMatchForBranchSpecificLabel(label)).Max();

        if (currentCommitTaggedVersion is not null)
        {
            SemanticVersionBuildMetaData semanticVersionBuildMetaData = new(
                versionSourceSha: Context.CurrentCommit.Sha,
                commitsSinceTag: null,
                branch: Context.CurrentBranch.Name.Friendly,
                commitSha: Context.CurrentCommit.Sha,
                commitShortSha: Context.CurrentCommit.Id.ToString(7),
                commitDate: Context.CurrentCommit.When,
                numberOfUnCommittedChanges: Context.NumberOfUncommittedChanges
            );

            var preReleaseTag = currentCommitTaggedVersion.Value.PreReleaseTag;
            if (effectiveConfiguration.DeploymentMode == DeploymentMode.ContinuousDeployment)
            {
                preReleaseTag = SemanticVersionPreReleaseTag.Empty;
            }

            result = new SemanticVersion(currentCommitTaggedVersion.Value)
            {
                PreReleaseTag = preReleaseTag,
                BuildMetaData = semanticVersionBuildMetaData
            };
        }

        return result is not null;
    }

    private SemanticVersion CalculateSemanticVersion(
        DeploymentMode deploymentMode, SemanticVersion semanticVersion, ICommit? baseVersionSource)
    {
        IDeploymentModeCalculator deploymentModeCalculator = deploymentMode switch
        {
            DeploymentMode.ManualDeployment => deploymentModeCalculators.SingleOfType<ManualDeploymentVersionCalculator>(),
            DeploymentMode.ContinuousDelivery => deploymentModeCalculators.SingleOfType<ContinuousDeliveryVersionCalculator>(),
            DeploymentMode.ContinuousDeployment => deploymentModeCalculators.SingleOfType<ContinuousDeploymentVersionCalculator>(),
            _ => throw new InvalidEnumArgumentException(nameof(deploymentMode), (int)deploymentMode, typeof(DeploymentMode))
        };
        return deploymentModeCalculator.Calculate(semanticVersion, baseVersionSource);
    }

    private NextVersion CalculateNextVersion(IBranch branch, IGitVersionConfiguration configuration)
    {
        var nextVersions = GetNextVersions(branch, configuration);
        log.Separator();

        var maxVersion = nextVersions.Max()
            ?? throw new GitVersionException("No base versions determined on the current branch.");

        ICommit? latestBaseVersionSource;

        var matchingVersionsOnceIncremented = nextVersions
            .Where(
                element => element.BaseVersion.BaseVersionSource != null
                    && element.IncrementedVersion == maxVersion.IncrementedVersion
            ).ToArray();
        if (matchingVersionsOnceIncremented.Length > 1)
        {
            var latestVersion = matchingVersionsOnceIncremented.Aggregate(CompareVersions);
            latestBaseVersionSource = latestVersion.BaseVersion.BaseVersionSource;
            maxVersion = latestVersion;
            log.Info(
                $"Found multiple base versions which will produce the same SemVer ({maxVersion.IncrementedVersion}), " +
                $"taking latest source for commit counting ({latestVersion.BaseVersion.Source})");
        }
        else
        {
            IEnumerable<NextVersion> filteredVersions = nextVersions;
            if (!maxVersion.IncrementedVersion.PreReleaseTag.HasTag())
            {
                // If the maximal version has no pre-release tag defined than we want to determine just the latest previous
                // base source which are not coming from pre-release tag.
                filteredVersions = filteredVersions.Where(v => !v.BaseVersion.SemanticVersion.PreReleaseTag.HasTag());
            }

            var versions = filteredVersions as NextVersion[] ?? filteredVersions.ToArray();
            var version = versions
                .Where(v => v.BaseVersion.BaseVersionSource != null)
                .OrderByDescending(v => v.IncrementedVersion)
                .ThenByDescending(v => v.BaseVersion.BaseVersionSource?.When)
                .FirstOrDefault();

            version ??= versions.Where(v => v.BaseVersion.BaseVersionSource == null)
                .OrderByDescending(v => v.IncrementedVersion)
                .First();
            latestBaseVersionSource = version.BaseVersion.BaseVersionSource;
        }

        BaseVersion calculatedBase = new()
        {
            Operand = new BaseVersionOperand
            {
                Source = maxVersion.BaseVersion.Source,
                BaseVersionSource = latestBaseVersionSource,
                SemanticVersion = maxVersion.BaseVersion.SemanticVersion
            }
        };

        log.Info($"Base version used: {calculatedBase}");
        log.Separator();

        return new(maxVersion.IncrementedVersion, calculatedBase, maxVersion.BranchConfiguration);
    }

    private static NextVersion CompareVersions(NextVersion version1, NextVersion version2)
    {
        if (version1.BaseVersion.BaseVersionSource == null)
            return version2;

        if (version2.BaseVersion.BaseVersionSource == null)
            return version1;

        return version1.BaseVersion.BaseVersionSource.When >= version2.BaseVersion.BaseVersionSource.When
            ? version1
            : version2;
    }

    private IReadOnlyCollection<NextVersion> GetNextVersions(IBranch branch, IGitVersionConfiguration configuration)
    {
        using (log.IndentLog("Fetching the base versions for version calculation..."))
        {
            if (branch.Tip == null)
                throw new GitVersionException("No commits found on the current branch.");

            return GetNextVersionsInternal().ToList();
        }

        IEnumerable<NextVersion> GetNextVersionsInternal()
        {
            var effectiveBranchConfigurations = this.effectiveBranchConfigurationFinder.GetConfigurations(branch, configuration).ToArray();
            foreach (var effectiveBranchConfiguration in effectiveBranchConfigurations)
            {
                using (this.log.IndentLog($"Calculating base versions for '{effectiveBranchConfiguration.Branch.Name}'"))
                {
                    var strategies = this.versionStrategies.ToList();
                    var fallbackVersionStrategy = strategies.Find(element => element is FallbackVersionStrategy);
                    if (fallbackVersionStrategy is not null)
                    {
                        strategies.Remove(fallbackVersionStrategy);
                        strategies.Add(fallbackVersionStrategy);
                    }

                    var atLeastOneBaseVersionReturned = false;
                    foreach (var versionStrategy in strategies)
                    {
                        if (atLeastOneBaseVersionReturned && versionStrategy is FallbackVersionStrategy) continue;

                        using (this.log.IndentLog($"[Using '{versionStrategy.GetType().Name}' strategy]"))
                        {
                            foreach (var baseVersion in versionStrategy.GetBaseVersions(effectiveBranchConfiguration))
                            {
                                log.Info(baseVersion.ToString());
                                if (IncludeVersion(baseVersion, configuration.Ignore))
                                {
                                    atLeastOneBaseVersionReturned = true;

                                    yield return new NextVersion(
                                        incrementedVersion: baseVersion.GetIncrementedVersion(),
                                        baseVersion: baseVersion,
                                        configuration: effectiveBranchConfiguration
                                    );
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private bool IncludeVersion(IBaseVersion baseVersion, IIgnoreConfiguration ignoreConfiguration)
    {
        foreach (var versionFilter in ignoreConfiguration.ToFilters())
        {
            if (versionFilter.Exclude(baseVersion, out var reason))
            {
                if (reason != null)
                {
                    log.Info(reason);
                }

                return false;
            }
        }

        return true;
    }
}
