using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Logging;

namespace GitVersion.VersionCalculation;

internal class NextVersionCalculator : INextVersionCalculator
{
    private readonly ILog log;
    private readonly IMainlineVersionCalculator mainlineVersionCalculator;
    private readonly IContinuousDeploymentVersionCalculator continuousDeploymentVersionCalculator;
    private readonly IContinuousDeliveryVersionCalculator continuousDeliveryVersionCalculator;
    private readonly IManualDeploymentVersionCalculator manualDeploymentVersionCalculator;
    private readonly Lazy<GitVersionContext> versionContext;
    private readonly IVersionStrategy[] versionStrategies;
    private readonly IEffectiveBranchConfigurationFinder effectiveBranchConfigurationFinder;
    private readonly IIncrementStrategyFinder incrementStrategyFinder;

    private GitVersionContext Context => this.versionContext.Value;

    public NextVersionCalculator(ILog log,
                                 Lazy<GitVersionContext> versionContext,
                                 IMainlineVersionCalculator mainlineVersionCalculator,
                                 IContinuousDeploymentVersionCalculator continuousDeploymentVersionCalculator,
                                 IContinuousDeliveryVersionCalculator continuousDeliveryVersionCalculator,
                                 IManualDeploymentVersionCalculator manualDeploymentVersionCalculator,
                                 IEnumerable<IVersionStrategy> versionStrategies,
                                 IEffectiveBranchConfigurationFinder effectiveBranchConfigurationFinder,
                                 IIncrementStrategyFinder incrementStrategyFinder)
    {
        this.log = log.NotNull();
        this.versionContext = versionContext.NotNull();
        this.mainlineVersionCalculator = mainlineVersionCalculator.NotNull();
        this.continuousDeploymentVersionCalculator = continuousDeploymentVersionCalculator.NotNull();
        this.continuousDeliveryVersionCalculator = continuousDeliveryVersionCalculator.NotNull();
        this.manualDeploymentVersionCalculator = manualDeploymentVersionCalculator.NotNull();
        this.incrementStrategyFinder = incrementStrategyFinder.NotNull();
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

        var nextVersion = Calculate(Context.CurrentBranch, Context.Configuration);
        var incrementedVersion = CalculateIncrementedVersion(nextVersion.Configuration.VersioningMode, nextVersion);
        return new(incrementedVersion, nextVersion.BaseVersion, new(nextVersion.Branch, nextVersion.Configuration));
    }

    private SemanticVersion CalculateIncrementedVersion(VersioningMode versioningMode, NextVersion nextVersion) => versioningMode switch
    {
        VersioningMode.ContinuousDelivery => this.manualDeploymentVersionCalculator.Calculate(nextVersion),
        VersioningMode.ContinuousDeployment => nextVersion.Configuration.IsMainline && nextVersion.Configuration.Label is null
            ? this.continuousDeploymentVersionCalculator.Calculate(nextVersion)
            : this.continuousDeliveryVersionCalculator.Calculate(nextVersion),
        VersioningMode.Mainline => this.mainlineVersionCalculator.FindMainlineModeVersion(nextVersion),
        _ => throw new InvalidEnumArgumentException(nameof(versioningMode), (int)versioningMode, typeof(VersioningMode)),
    };

    private NextVersion Calculate(IBranch branch, IGitVersionConfiguration configuration)
    {
        using (log.IndentLog("Calculating the base versions"))
        {
            var nextVersions = GetNextVersions(branch, configuration).ToArray();
            var maxVersion = nextVersions.Max()!;

            var matchingVersionsOnceIncremented = nextVersions
                .Where(v => v.BaseVersion.BaseVersionSource != null && v.IncrementedVersion == maxVersion.IncrementedVersion)
                .ToList();
            ICommit? latestBaseVersionSource;

            if (matchingVersionsOnceIncremented.Any())
            {
                static NextVersion CompareVersions(
                    NextVersion versions1,
                    NextVersion version2)
                {
                    if (versions1.BaseVersion.BaseVersionSource == null)
                    {
                        return version2;
                    }
                    if (version2.BaseVersion.BaseVersionSource == null)
                    {
                        return versions1;
                    }

                    return versions1.BaseVersion.BaseVersionSource.When
                        < version2.BaseVersion.BaseVersionSource.When ? versions1 : version2;
                }

                var latestVersion = matchingVersionsOnceIncremented.Aggregate(CompareVersions);
                latestBaseVersionSource = latestVersion.BaseVersion.BaseVersionSource;
                maxVersion = latestVersion;
                log.Info($"Found multiple base versions which will produce the same SemVer ({maxVersion.IncrementedVersion})," +
                    $" taking oldest source for commit counting ({latestVersion.BaseVersion.Source})");
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

            var calculatedBase = new BaseVersion(
                maxVersion.BaseVersion.Source,
                maxVersion.BaseVersion.ShouldIncrement,
                maxVersion.BaseVersion.SemanticVersion,
                latestBaseVersionSource,
                maxVersion.BaseVersion.BranchNameOverride
            );

            log.Info($"Base version used: {calculatedBase}");

            return new NextVersion(maxVersion.IncrementedVersion, calculatedBase, maxVersion.Branch, maxVersion.Configuration);
        }
    }

    private IReadOnlyCollection<NextVersion> GetNextVersions(IBranch branch, IGitVersionConfiguration configuration)
    {
        if (branch.Tip == null)
            throw new GitVersionException("No commits found on the current branch.");

        var nextVersions = GetNextVersionsInternal().ToList();
        if (nextVersions.Count == 0)
            throw new GitVersionException("No base versions determined on the current branch.");
        return nextVersions;

        IEnumerable<NextVersion> GetNextVersionsInternal()
        {
            foreach (var effectiveConfiguration in effectiveBranchConfigurationFinder.GetConfigurations(branch, configuration))
            {
                var atLeastOneBaseVersionReturned = false;
                foreach (var versionStrategy in this.versionStrategies)
                {
                    foreach (var baseVersion in versionStrategy.GetBaseVersions(effectiveConfiguration))
                    {
                        log.Info(baseVersion.ToString());

                        if (IncludeVersion(baseVersion, configuration.Ignore)
                            && TryGetNextVersion(out var nextVersion, effectiveConfiguration, baseVersion))
                        {
                            yield return nextVersion;
                            atLeastOneBaseVersionReturned = true;
                        }
                    }
                }

                if (!atLeastOneBaseVersionReturned)
                {
                    var baseVersion = new BaseVersion("Fallback base version", true, SemanticVersion.Empty, null, null);
                    if (TryGetNextVersion(out var nextVersion, effectiveConfiguration, baseVersion)) yield return nextVersion;
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
            log.Info("Using current branch name to calculate version tag");

        var incrementedVersion = GetIncrementedVersion(effectiveConfiguration, baseVersion, label);
        if (incrementedVersion.IsMatchForBranchSpecificLabel(label))
        {
            result = effectiveConfiguration.CreateNextVersion(baseVersion, incrementedVersion);
        }
        return result is not null;
    }

    private SemanticVersion GetIncrementedVersion(EffectiveBranchConfiguration configuration, BaseVersion baseVersion, string? label)
    {
        var incrementStrategy = incrementStrategyFinder.DetermineIncrementedField(
            currentCommit: Context.CurrentCommit,
            baseVersion: baseVersion,
            configuration: configuration.Value
        );
        return baseVersion.SemanticVersion.IncrementVersion(incrementStrategy, label);
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
