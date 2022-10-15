using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.Helpers;

internal abstract class TestConfigurationBuilderBase<TConfigurationBuilder>
    where TConfigurationBuilder : TestConfigurationBuilderBase<TConfigurationBuilder>
{
    private AssemblyVersioningScheme? assemblyVersioningScheme;
    private AssemblyFileVersioningScheme? assemblyFileVersioningScheme;
    private string? assemblyInformationalFormat;
    private string? assemblyVersioningFormat;
    private string? assemblyFileVersioningFormat;
    private VersioningMode? versioningMode;
    private string? tagPrefix;
    private string? continuousDeploymentFallbackTag;
    private string? nextVersion;
    private string? majorVersionBumpMessage;
    private string? minorVersionBumpMessage;
    private string? patchVersionBumpMessage;
    private string? noBumpMessage;
    private int? tagPreReleaseWeight;
    private CommitMessageIncrementMode? commitMessageIncrementing;
    private IgnoreConfiguration ignore;
    private IncrementStrategy? increment;
    private string? commitDateFormat;
    private bool? updateBuildNumber;
    private SemanticVersionFormat semanticVersionFormat = SemanticVersionFormat.Strict;
    private Dictionary<string, string>? mergeMessageFormats;
    protected readonly Dictionary<string, TestBranchConfigurationBuilder> branchConfigurationBuilders = new();

    protected TestConfigurationBuilderBase()
    {
        if (GetType() != typeof(TConfigurationBuilder))
        {
            throw new ArgumentException("The generic type parameter is not equal to the instance type.");
        }
    }

    public virtual TConfigurationBuilder WithAssemblyVersioningScheme(AssemblyVersioningScheme? value)
    {
        this.assemblyVersioningScheme = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithAssemblyFileVersioningScheme(AssemblyFileVersioningScheme? value)
    {
        this.assemblyFileVersioningScheme = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithAssemblyInformationalFormat(string? value)
    {
        this.assemblyInformationalFormat = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithAssemblyVersioningFormat(string? value)
    {
        this.assemblyVersioningFormat = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithAssemblyFileVersioningFormat(string? value)
    {
        this.assemblyFileVersioningFormat = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithoutVersioningMode()
    {
        this.versioningMode = null;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithVersioningMode(VersioningMode value)
    {
        this.versioningMode = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithTagPrefix(string? value)
    {
        this.tagPrefix = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithContinuousDeploymentFallbackTag(string? value)
    {
        this.continuousDeploymentFallbackTag = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithNextVersion(string? value)
    {
        this.nextVersion = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithMajorVersionBumpMessage(string? value)
    {
        this.majorVersionBumpMessage = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithMinorVersionBumpMessage(string? value)
    {
        this.minorVersionBumpMessage = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithPatchVersionBumpMessage(string? value)
    {
        this.patchVersionBumpMessage = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithNoBumpMessage(string? value)
    {
        this.noBumpMessage = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithTagPreReleaseWeight(int? value)
    {
        this.tagPreReleaseWeight = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithCommitMessageIncrementing(CommitMessageIncrementMode? value)
    {
        this.commitMessageIncrementing = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithIgnoreConfiguration(IgnoreConfiguration value)
    {
        this.ignore = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithIncrement(IncrementStrategy? value)
    {
        this.increment = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithCommitDateFormat(string? value)
    {
        this.commitDateFormat = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithUpdateBuildNumber(bool? value)
    {
        this.updateBuildNumber = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithSemanticVersionFormat(SemanticVersionFormat value)
    {
        this.semanticVersionFormat = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithMergeMessageFormats(Dictionary<string, string> value)
    {
        this.mergeMessageFormats = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TestBranchConfigurationBuilder WithBranch(string value)
    {
        var result = this.branchConfigurationBuilders.GetOrAdd(value, () => TestBranchConfigurationBuilder.New);
        result.WithName(value);
        return result;
    }

    public virtual TConfigurationBuilder WithBranch(string value, Action<TestBranchConfigurationBuilder> action)
    {
        var result = this.branchConfigurationBuilders.GetOrAdd(value, () => TestBranchConfigurationBuilder.New);
        result.WithName(value);
        action(result);
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithConfiguration(GitVersionConfiguration value)
    {
        WithAssemblyVersioningScheme(value.AssemblyVersioningScheme);
        WithAssemblyFileVersioningScheme(value.AssemblyFileVersioningScheme);
        WithAssemblyInformationalFormat(value.AssemblyInformationalFormat);
        WithAssemblyVersioningFormat(value.AssemblyVersioningFormat);
        WithAssemblyFileVersioningFormat(value.AssemblyFileVersioningFormat);
        if (value.VersioningMode.HasValue)
        {
            WithVersioningMode(value.VersioningMode.Value);
        }
        else
        {
            WithoutVersioningMode();
        }
        WithTagPrefix(value.TagPrefix);
        WithContinuousDeploymentFallbackTag(value.ContinuousDeploymentFallbackTag);
        WithNextVersion(value.NextVersion);
        WithMajorVersionBumpMessage(value.MajorVersionBumpMessage);
        WithMinorVersionBumpMessage(value.MinorVersionBumpMessage);
        WithPatchVersionBumpMessage(value.PatchVersionBumpMessage);
        WithNoBumpMessage(value.NoBumpMessage);
        WithTagPreReleaseWeight(value.TagPreReleaseWeight);
        WithCommitMessageIncrementing(value.CommitMessageIncrementing);
        WithIgnoreConfiguration(value.Ignore);
        WithIncrement(value.Increment);
        WithCommitDateFormat(value.CommitDateFormat);
        WithUpdateBuildNumber(value.UpdateBuildNumber);
        WithSemanticVersionFormat(value.SemanticVersionFormat);
        WithMergeMessageFormats(value.MergeMessageFormats);
        foreach (var item in value.Branches)
        {
            WithBranch(item.Key).WithConfiguration(item.Value);
        }
        return (TConfigurationBuilder)this;
    }

    public virtual GitVersionConfiguration Build()
    {
        GitVersionConfiguration result = new()
        {
            AssemblyVersioningScheme = this.assemblyVersioningScheme,
            AssemblyFileVersioningScheme = this.assemblyFileVersioningScheme,
            AssemblyInformationalFormat = this.assemblyInformationalFormat,
            AssemblyVersioningFormat = this.assemblyVersioningFormat,
            AssemblyFileVersioningFormat = this.assemblyFileVersioningFormat,
            VersioningMode = this.versioningMode,
            TagPrefix = this.tagPrefix,
            ContinuousDeploymentFallbackTag = this.continuousDeploymentFallbackTag,
            NextVersion = this.nextVersion,
            MajorVersionBumpMessage = this.majorVersionBumpMessage,
            MinorVersionBumpMessage = this.minorVersionBumpMessage,
            PatchVersionBumpMessage = this.patchVersionBumpMessage,
            NoBumpMessage = this.noBumpMessage,
            TagPreReleaseWeight = this.tagPreReleaseWeight,
            CommitMessageIncrementing = this.commitMessageIncrementing,
            Ignore = this.ignore,
            Increment = this.increment,
            CommitDateFormat = this.commitDateFormat,
            UpdateBuildNumber = this.updateBuildNumber,
            SemanticVersionFormat = this.semanticVersionFormat,
            MergeMessageFormats = this.mergeMessageFormats ?? new()
        };
        Dictionary<string, BranchConfiguration> branches = new();
        foreach (var item in this.branchConfigurationBuilders)
        {
            branches.Add(item.Key, item.Value.Build());
        }
        result.Branches = branches;
        return result;
    }

    public record BranchMetaData
    {
        public string Name { get; init; }

        public string RegexPattern { get; init; }
    }
}
