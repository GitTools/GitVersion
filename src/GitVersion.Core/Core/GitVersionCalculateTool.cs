using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.Caching;
using Microsoft.Extensions.Options;

namespace GitVersion;

internal class GitVersionCalculateTool(
    ILog log,
    INextVersionCalculator nextVersionCalculator,
    IVariableProvider variableProvider,
    IGitPreparer gitPreparer,
    IGitVersionCache gitVersionCache,
    IGitVersionCacheKeyFactory cacheKeyFactory,
    IOptions<GitVersionOptions> options,
    Lazy<GitVersionContext> versionContext)
    : IGitVersionCalculateTool
{
    private readonly ILog log = log.NotNull();
    private readonly IGitVersionCache gitVersionCache = gitVersionCache.NotNull();
    private readonly INextVersionCalculator nextVersionCalculator = nextVersionCalculator.NotNull();
    private readonly IVariableProvider variableProvider = variableProvider.NotNull();
    private readonly IGitPreparer gitPreparer = gitPreparer.NotNull();
    private readonly IGitVersionCacheKeyFactory cacheKeyFactory = cacheKeyFactory.NotNull();

    private readonly IOptions<GitVersionOptions> options = options.NotNull();
    private readonly Lazy<GitVersionContext> versionContext = versionContext.NotNull();

    private GitVersionContext Context => this.versionContext.Value;

    public GitVersionVariables CalculateVersionVariables()
    {
        this.gitPreparer.Prepare(); //we need to prepare the repository before using it for version calculation

        var gitVersionOptions = this.options.Value;

        var cacheKey = this.cacheKeyFactory.Create(gitVersionOptions.ConfigurationInfo.OverrideConfiguration);
        var versionVariables = gitVersionOptions.Settings.NoCache ? default : this.gitVersionCache.LoadVersionVariablesFromDiskCache(cacheKey);

        if (versionVariables != null) return versionVariables;

        var semanticVersion = this.nextVersionCalculator.FindVersion();

        var branchConfiguration = Context.Configuration.GetBranchConfiguration(Context.CurrentBranch);
        EffectiveConfiguration effectiveConfiguration = new(Context.Configuration, branchConfiguration);
        versionVariables = this.variableProvider.GetVariablesFor(
            semanticVersion, Context.Configuration, effectiveConfiguration.PreReleaseWeight);

        if (gitVersionOptions.Settings.NoCache) return versionVariables;
        try
        {
            this.gitVersionCache.WriteVariablesToDiskCache(cacheKey, versionVariables);
        }
        catch (AggregateException e)
        {
            this.log.Warning($"One or more exceptions during cache write:{PathHelper.NewLine}{e}");
        }

        return versionVariables;
    }
}
