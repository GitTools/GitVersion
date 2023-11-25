using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal class NextVersionCalculator : INextVersionCalculator
{
    private readonly ILog log;
    private readonly Lazy<GitVersionContext> versionContext;
    private readonly IEnumerable<IVersionModeCalculator> versionModeCalculators;
    private readonly IVersionStrategy[] versionStrategies;
    private readonly IEffectiveBranchConfigurationFinder effectiveBranchConfigurationFinder;
    private readonly IIncrementStrategyFinder incrementStrategyFinder;

    private GitVersionContext Context => this.versionContext.Value;

    public NextVersionCalculator(ILog log,
                                 Lazy<GitVersionContext> versionContext,
                                 IEnumerable<IVersionModeCalculator> versionModeCalculators,
                                 IEnumerable<IVersionStrategy> versionStrategies,
                                 IEffectiveBranchConfigurationFinder effectiveBranchConfigurationFinder,
                                 IIncrementStrategyFinder incrementStrategyFinder)
    {
        this.log = log.NotNull();
        this.versionContext = versionContext.NotNull();
        this.versionModeCalculators = versionModeCalculators;
        this.versionStrategies = versionStrategies.NotNull().ToArray();
        this.effectiveBranchConfigurationFinder = effectiveBranchConfigurationFinder.NotNull();
        this.incrementStrategyFinder = incrementStrategyFinder.NotNull();
    }

    public virtual NextVersion FindVersion()
    {
        this.log.Info($"Running against branch: {Context.CurrentBranch} ({Context.CurrentCommit?.ToString() ?? "-"})");
        if (Context.IsCurrentCommitTagged)
        {
            this.log.Info($"Current commit is tagged with version {Context.CurrentCommitTaggedVersion}, version calculation is for meta data only.");
        }

        var nextVersion = CalculateNextVersion(Context.CurrentBranch, Context.Configuration);
        var incrementedVersion = CalculateIncrementedVersion(nextVersion.Configuration.VersioningMode, nextVersion);

        return CreateNextVersion(nextVersion.BaseVersion, incrementedVersion, nextVersion.BranchConfiguration);
    }

    private SemanticVersion CalculateIncrementedVersion(VersioningMode versioningMode, NextVersion nextVersion)
    {
        IVersionModeCalculator calculator = versioningMode switch
        {
            VersioningMode.ManualDeployment => this.versionModeCalculators.SingleOfType<ManualDeploymentVersionCalculator>(),
            VersioningMode.ContinuousDelivery => this.versionModeCalculators.SingleOfType<ManualDeploymentVersionCalculator>(),
            VersioningMode.ContinuousDeployment => nextVersion.Configuration is { IsMainline: true, Label: null }
                ? this.versionModeCalculators.SingleOfType<ContinuousDeploymentVersionCalculator>()
                : this.versionModeCalculators.SingleOfType<ContinuousDeliveryVersionCalculator>(),
            VersioningMode.Mainline => this.versionModeCalculators.SingleOfType<MainlineVersionCalculator>(),
            _ => throw new InvalidEnumArgumentException(nameof(versioningMode), (int)versioningMode, typeof(VersioningMode)),
        };
        return calculator.Calculate(nextVersion);
    }

    private NextVersion CalculateNextVersion(IBranch branch, IGitVersionConfiguration configuration)
    {
        var nextVersions = GetNextVersions(branch, configuration).ToArray();
        log.Separator();
        var maxVersion = nextVersions.Max()!;

        var matchingVersionsOnceIncremented = nextVersions
            .Where(v => v.BaseVersion.BaseVersionSource != null && v.IncrementedVersion == maxVersion.IncrementedVersion)
            .ToList();
        ICommit? latestBaseVersionSource;

        if (matchingVersionsOnceIncremented.Any())
        {
            var latestVersion = matchingVersionsOnceIncremented.Aggregate(CompareVersions);
            latestBaseVersionSource = latestVersion.BaseVersion.BaseVersionSource;
            maxVersion = latestVersion;
            log.Info(
                $"Found multiple base versions which will produce the same SemVer ({maxVersion.IncrementedVersion}), " +
                $"taking oldest source for commit counting ({latestVersion.BaseVersion.Source})");
        }
        else
        {
            IEnumerable<NextVersion> filteredVersions = nextVersions;
            if (!maxVersion.IncrementedVersion.PreReleaseTag.HasTag())
            {
                // If the maximal version has no pre-release tag defined than we want to determine just the latest previous
                // base source which are not coming from pre-release tag.
                filteredVersions = filteredVersions.Where(v => !v.BaseVersion.GetSemanticVersion().PreReleaseTag.HasTag());
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

        var calculatedBase = new BaseVersion(
            maxVersion.BaseVersion.Source,
            maxVersion.BaseVersion.ShouldIncrement,
            maxVersion.BaseVersion.GetSemanticVersion(),
            latestBaseVersionSource,
            maxVersion.BaseVersion.BranchNameOverride
        );

        log.Info($"Base version used: {calculatedBase}");
        log.Separator();
        return CreateNextVersion(calculatedBase, maxVersion.IncrementedVersion, maxVersion.BranchConfiguration);
    }

    private static NextVersion CreateNextVersion(BaseVersion baseVersion, SemanticVersion incrementedVersion, EffectiveBranchConfiguration effectiveBranchConfiguration)
    {
        incrementedVersion.NotNull();
        baseVersion.NotNull();

        return new(incrementedVersion, baseVersion, effectiveBranchConfiguration);
    }

    private static NextVersion CompareVersions(NextVersion versions1, NextVersion version2)
    {
        if (versions1.BaseVersion.BaseVersionSource == null)
            return version2;

        if (version2.BaseVersion.BaseVersionSource == null)
            return versions1;

        return versions1.BaseVersion.BaseVersionSource.When < version2.BaseVersion.BaseVersionSource.When
            ? versions1
            : version2;
    }

    private IReadOnlyCollection<NextVersion> GetNextVersions(IBranch branch, IGitVersionConfiguration configuration)
    {
        using (log.IndentLog("Fetching the base versions for version calculation..."))
        {
            if (branch.Tip == null)
                throw new GitVersionException("No commits found on the current branch.");

            var nextVersions = GetNextVersionsInternal().ToList();
            if (nextVersions.Count == 0)
                throw new GitVersionException("No base versions determined on the current branch.");
            return nextVersions;
        }

        IEnumerable<NextVersion> GetNextVersionsInternal()
        {
            var effectiveBranchConfigurations = this.effectiveBranchConfigurationFinder.GetConfigurations(branch, configuration).ToArray();
            foreach (var effectiveBranchConfiguration in effectiveBranchConfigurations)
            {
                this.log.Info($"Calculating base versions for '{effectiveBranchConfiguration.Branch.Name}'");
                var atLeastOneBaseVersionReturned = false;
                foreach (var versionStrategy in this.versionStrategies)
                {
                    using (this.log.IndentLog($"[Using '{versionStrategy.GetType().Name}' strategy]"))
                    {
                        foreach (var baseVersion in versionStrategy.GetBaseVersions(effectiveBranchConfiguration))
                        {
                            log.Info(baseVersion.ToString());
                            if (IncludeVersion(baseVersion, configuration.Ignore)
                                && TryGetNextVersion(out var nextVersion, effectiveBranchConfiguration, baseVersion))
                            {
                                yield return nextVersion;
                                atLeastOneBaseVersionReturned = true;
                            }
                        }
                    }
                }

                if (!atLeastOneBaseVersionReturned)
                {
                    var baseVersion = new BaseVersion("Fallback base version", true, SemanticVersion.Empty, null, null);
                    if (TryGetNextVersion(out var nextVersion, effectiveBranchConfiguration, baseVersion))
                        yield return nextVersion;
                }
            }
        }
    }

    private bool TryGetNextVersion([NotNullWhen(true)] out NextVersion? result,
                                   EffectiveBranchConfiguration effectiveConfiguration, BaseVersion baseVersion)
    {
        result = null;

        var label = effectiveConfiguration.Value.GetBranchSpecificLabel(
            Context.CurrentBranch.Name, baseVersion.BranchNameOverride
        );
        if (effectiveConfiguration.Value.Label != label)
        {
            log.Info("Using current branch name to calculate version tag");
        }

        var incrementedVersion = GetIncrementedVersion(effectiveConfiguration, baseVersion, label);
        if (incrementedVersion.IsMatchForBranchSpecificLabel(label))
        {
            result = effectiveConfiguration.CreateNextVersion(baseVersion, incrementedVersion);
        }

        return result is not null;
    }

    //private SemanticVersion GetIncrementedVersion(EffectiveBranchConfiguration configuration, BaseVersion baseVersion, string? label)
    //{
    //    var incrementStrategy = incrementStrategyFinder.DetermineIncrementedField(
    //        currentCommit: Context.CurrentCommit,
    //        baseVersion: baseVersion,
    //        configuration: configuration.Value
    //    );
    //    return baseVersion.GetSemanticVersion().IncrementVersion(incrementStrategy, label);
    //}

    private SemanticVersion GetIncrementedVersion(EffectiveBranchConfiguration configuration, BaseVersion baseVersion, string? label)
    {
        if (baseVersion is BaseVersionV2 baseVersionV2)
        {
            if (baseVersion.ShouldIncrement)
            {
                SemanticVersion result = baseVersionV2.GetSemanticVersion().Increment(
                   baseVersionV2.Increment, baseVersionV2.Label, baseVersionV2.ForceIncrement
               );

                if (result.IsLessThan(baseVersionV2.AlternativeSemanticVersion, includePreRelease: false))
                {
                    result = new SemanticVersion(result)
                    {
                        Major = baseVersionV2.AlternativeSemanticVersion!.Major,
                        Minor = baseVersionV2.AlternativeSemanticVersion.Minor,
                        Patch = baseVersionV2.AlternativeSemanticVersion.Patch
                    };
                }
                return result;
            }
            else
            {
                return baseVersion.GetSemanticVersion();
            }
        }
        else
        {
            var incrementStrategy = incrementStrategyFinder.DetermineIncrementedField(
                currentCommit: Context.CurrentCommit,
                baseVersion: baseVersion,
                configuration: configuration.Value
            );
            return baseVersion.GetSemanticVersion().Increment(incrementStrategy, label);
        }
    }

    private bool IncludeVersion(BaseVersion baseVersion, IIgnoreConfiguration ignoreConfiguration)
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
