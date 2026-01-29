using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;
using GitVersion.VersionCalculation.Caching;

namespace GitVersion;

internal class GitVersionCalculateTool(
    ILogger<GitVersionCalculateTool> logger,
    INextVersionCalculator nextVersionCalculator,
    IVariableProvider variableProvider,
    IGitPreparer gitPreparer,
    IGitVersionCacheProvider gitVersionCacheProvider,
    IOptions<GitVersionOptions> options,
    Lazy<GitVersionContext> versionContext)
    : IGitVersionCalculateTool
{
    private readonly ILogger<GitVersionCalculateTool> logger = logger.NotNull();
    private readonly IGitVersionCacheProvider gitVersionCacheProvider = gitVersionCacheProvider.NotNull();
    private readonly INextVersionCalculator nextVersionCalculator = nextVersionCalculator.NotNull();
    private readonly IVariableProvider variableProvider = variableProvider.NotNull();
    private readonly IGitPreparer gitPreparer = gitPreparer.NotNull();

    private readonly IOptions<GitVersionOptions> options = options.NotNull();
    private readonly Lazy<GitVersionContext> versionContext = versionContext.NotNull();

    private GitVersionContext Context => this.versionContext.Value;

    public GitVersionVariables CalculateVersionVariables()
    {
        this.gitPreparer.Prepare(); //we need to prepare the repository before using it for version calculation

        var gitVersionOptions = this.options.Value;

        var versionVariables = !gitVersionOptions.Settings.NoCache
            ? this.gitVersionCacheProvider.LoadVersionVariablesFromDiskCache()
            : null;

        if (versionVariables != null) return versionVariables;

        var semanticVersion = this.nextVersionCalculator.FindVersion();

        var branchConfiguration = Context.Configuration.GetBranchConfiguration(Context.CurrentBranch);
        EffectiveConfiguration effectiveConfiguration = new(Context.Configuration, branchConfiguration);
        versionVariables = this.variableProvider.GetVariablesFor(
            semanticVersion, Context.Configuration, effectiveConfiguration.PreReleaseWeight);

        if (gitVersionOptions.Settings.NoCache) return versionVariables;
        try
        {
            this.gitVersionCacheProvider.WriteVariablesToDiskCache(versionVariables);
        }
        catch (AggregateException ex)
        {
            this.logger.LogError(ex, "One or more exceptions during cache write:{NewLine}", FileSystemHelper.Path.NewLine);
        }

        return versionVariables;
    }
}
