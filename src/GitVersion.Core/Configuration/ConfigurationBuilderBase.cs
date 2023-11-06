using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

internal abstract class ConfigurationBuilderBase<TConfigurationBuilder> : IConfigurationBuilder
    where TConfigurationBuilder : ConfigurationBuilderBase<TConfigurationBuilder>
{
    private AssemblyVersioningScheme? assemblyVersioningScheme;
    private AssemblyFileVersioningScheme? assemblyFileVersioningScheme;
    private string? assemblyInformationalFormat;
    private string? assemblyVersioningFormat;
    private string? assemblyFileVersioningFormat;
    private string? tagPrefix;
    private string? versionInBranchPattern;
    private string? nextVersion;
    private string? majorVersionBumpMessage;
    private string? minorVersionBumpMessage;
    private string? patchVersionBumpMessage;
    private string? noBumpMessage;
    private int? tagPreReleaseWeight;
    private IgnoreConfiguration ignore;
    private string? commitDateFormat;
    private bool updateBuildNumber;
    private SemanticVersionFormat semanticVersionFormat;
    private Dictionary<string, string> mergeMessageFormats = new();
    private readonly List<IReadOnlyDictionary<object, object?>> overrides = new();
    private readonly Dictionary<string, BranchConfigurationBuilder> branchConfigurationBuilders = new();
    private VersioningMode? versioningMode;
    private string? label;
    private IncrementStrategy increment = IncrementStrategy.Inherit;
    private bool? preventIncrementOfMergedBranchVersion;
    private string? labelNumberPattern;
    private bool? trackMergeTarget;
    private bool? trackMergeMessage;
    private CommitMessageIncrementMode? commitMessageIncrementing;
    private string? regularExpression;
    private bool? tracksReleaseBranches;
    private bool? isReleaseBranch;
    private bool? isMainline;
    private int? preReleaseWeight;

    protected readonly BranchMetaData MainBranch = new()
    {
        Name = ConfigurationConstants.MainBranchKey,
        RegexPattern = ConfigurationConstants.MainBranchRegex
    };

    protected readonly BranchMetaData DevelopBranch = new()
    {
        Name = ConfigurationConstants.DevelopBranchKey,
        RegexPattern = ConfigurationConstants.DevelopBranchRegex
    };

    protected readonly BranchMetaData ReleaseBranch = new()
    {
        Name = ConfigurationConstants.ReleaseBranchKey,
        RegexPattern = ConfigurationConstants.ReleaseBranchRegex
    };

    protected readonly BranchMetaData FeatureBranch = new()
    {
        Name = ConfigurationConstants.FeatureBranchKey,
        RegexPattern = ConfigurationConstants.FeatureBranchRegex
    };

    protected readonly BranchMetaData PullRequestBranch = new()
    {
        Name = ConfigurationConstants.PullRequestBranchKey,
        RegexPattern = ConfigurationConstants.PullRequestBranchRegex
    };

    protected readonly BranchMetaData HotfixBranch = new()
    {
        Name = ConfigurationConstants.HotfixBranchKey,
        RegexPattern = ConfigurationConstants.HotfixBranchRegex
    };

    protected readonly BranchMetaData SupportBranch = new()
    {
        Name = ConfigurationConstants.SupportBranchKey,
        RegexPattern = ConfigurationConstants.SupportBranchRegex
    };

    protected readonly BranchMetaData UnknownBranch = new()
    {
        Name = ConfigurationConstants.UnknownBranchKey,
        RegexPattern = ConfigurationConstants.UnknownBranchRegex
    };

    protected ConfigurationBuilderBase()
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

    public virtual TConfigurationBuilder WithTagPrefix(string? value)
    {
        this.tagPrefix = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithVersionInBranchPattern(string? value)
    {
        this.versionInBranchPattern = value;
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

    public virtual TConfigurationBuilder WithIgnoreConfiguration(IIgnoreConfiguration value)
    {
        this.ignore = (IgnoreConfiguration)value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithCommitDateFormat(string? value)
    {
        this.commitDateFormat = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithUpdateBuildNumber(bool value)
    {
        this.updateBuildNumber = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithSemanticVersionFormat(SemanticVersionFormat value)
    {
        this.semanticVersionFormat = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithMergeMessageFormats(IReadOnlyDictionary<string, string> value)
    {
        this.mergeMessageFormats = new(value);
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithoutBranches()
    {
        this.branchConfigurationBuilders.Clear();
        return (TConfigurationBuilder)this;
    }

    public virtual BranchConfigurationBuilder WithBranch(string value)
        => this.branchConfigurationBuilders.GetOrAdd(value, () => BranchConfigurationBuilder.New);

    public virtual BranchConfigurationBuilder WithBranch(string value, BranchConfigurationBuilder builder)
        => this.branchConfigurationBuilders.GetOrAdd(value, () => builder);

    public virtual TConfigurationBuilder WithBranch(string value, Action<BranchConfigurationBuilder> action)
    {
        var result = this.branchConfigurationBuilders.GetOrAdd(value, () => BranchConfigurationBuilder.New);
        action(result);
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithVersioningMode(VersioningMode? value)
    {
        this.versioningMode = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithLabel(string? value)
    {
        this.label = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithIncrement(IncrementStrategy value)
    {
        this.increment = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithPreventIncrementOfMergedBranchVersion(bool? value)
    {
        this.preventIncrementOfMergedBranchVersion = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithLabelNumberPattern(string? value)
    {
        this.labelNumberPattern = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithTrackMergeTarget(bool? value)
    {
        this.trackMergeTarget = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithTrackMergeMessage(bool? value)
    {
        this.trackMergeMessage = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithCommitMessageIncrementing(CommitMessageIncrementMode? value)
    {
        this.commitMessageIncrementing = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithRegularExpression(string? value)
    {
        this.regularExpression = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithTracksReleaseBranches(bool? value)
    {
        this.tracksReleaseBranches = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithIsReleaseBranch(bool? value)
    {
        this.isReleaseBranch = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithIsMainline(bool? value)
    {
        this.isMainline = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithPreReleaseWeight(int? value)
    {
        this.preReleaseWeight = value;
        return (TConfigurationBuilder)this;
    }

    public virtual TConfigurationBuilder WithConfiguration(IGitVersionConfiguration value)
    {
        WithAssemblyVersioningScheme(value.AssemblyVersioningScheme);
        WithAssemblyFileVersioningScheme(value.AssemblyFileVersioningScheme);
        WithAssemblyInformationalFormat(value.AssemblyInformationalFormat);
        WithAssemblyVersioningFormat(value.AssemblyVersioningFormat);
        WithAssemblyFileVersioningFormat(value.AssemblyFileVersioningFormat);
        WithTagPrefix(value.TagPrefix);
        WithVersionInBranchPattern(value.VersionInBranchPattern);
        WithNextVersion(value.NextVersion);
        WithMajorVersionBumpMessage(value.MajorVersionBumpMessage);
        WithMinorVersionBumpMessage(value.MinorVersionBumpMessage);
        WithPatchVersionBumpMessage(value.PatchVersionBumpMessage);
        WithNoBumpMessage(value.NoBumpMessage);
        WithTagPreReleaseWeight(value.TagPreReleaseWeight);
        WithIgnoreConfiguration(value.Ignore);
        WithCommitDateFormat(value.CommitDateFormat);
        WithUpdateBuildNumber(value.UpdateBuildNumber);
        WithSemanticVersionFormat(value.SemanticVersionFormat);
        WithMergeMessageFormats(value.MergeMessageFormats);
        foreach (var (name, branchConfiguration) in value.Branches)
        {
            WithBranch(name).WithConfiguration(branchConfiguration);
        }
        WithVersioningMode(value.VersioningMode);
        WithLabel(value.Label);
        WithIncrement(value.Increment);
        WithPreventIncrementOfMergedBranchVersion(value.PreventIncrementOfMergedBranchVersion);
        WithLabelNumberPattern(value.LabelNumberPattern);
        WithTrackMergeTarget(value.TrackMergeTarget);
        WithTrackMergeMessage(value.TrackMergeMessage);
        WithCommitMessageIncrementing(value.CommitMessageIncrementing);
        WithRegularExpression(value.RegularExpression);
        WithTracksReleaseBranches(value.TracksReleaseBranches);
        WithIsReleaseBranch(value.IsReleaseBranch);
        WithIsMainline(value.IsMainline);
        WithPreReleaseWeight(value.PreReleaseWeight);
        return (TConfigurationBuilder)this;
    }

    public void AddOverride(IReadOnlyDictionary<object, object?> value)
    {
        if (value.Any())
        {
            this.overrides.Add(value);
        }
    }

    public virtual IGitVersionConfiguration Build()
    {
        Dictionary<string, BranchConfiguration> branches = new();
        foreach (var (name, branchConfigurationBuilder) in this.branchConfigurationBuilders)
        {
            branches.Add(name, (BranchConfiguration)branchConfigurationBuilder.Build());
        }

        IGitVersionConfiguration configuration = new GitVersionConfiguration
        {
            AssemblyVersioningScheme = this.assemblyVersioningScheme,
            AssemblyFileVersioningScheme = this.assemblyFileVersioningScheme,
            AssemblyInformationalFormat = this.assemblyInformationalFormat,
            AssemblyVersioningFormat = this.assemblyVersioningFormat,
            AssemblyFileVersioningFormat = this.assemblyFileVersioningFormat,
            TagPrefix = this.tagPrefix,
            VersionInBranchPattern = this.versionInBranchPattern,
            NextVersion = this.nextVersion,
            MajorVersionBumpMessage = this.majorVersionBumpMessage,
            MinorVersionBumpMessage = this.minorVersionBumpMessage,
            PatchVersionBumpMessage = this.patchVersionBumpMessage,
            NoBumpMessage = this.noBumpMessage,
            TagPreReleaseWeight = this.tagPreReleaseWeight,
            Ignore = this.ignore,
            CommitDateFormat = this.commitDateFormat,
            UpdateBuildNumber = this.updateBuildNumber,
            SemanticVersionFormat = this.semanticVersionFormat,
            Branches = branches,
            MergeMessageFormats = this.mergeMessageFormats,
            VersioningMode = this.versioningMode,
            Label = this.label,
            Increment = this.increment,
            RegularExpression = this.regularExpression,
            TracksReleaseBranches = this.tracksReleaseBranches,
            TrackMergeTarget = this.trackMergeTarget,
            TrackMergeMessage = this.trackMergeMessage,
            CommitMessageIncrementing = this.commitMessageIncrementing,
            IsMainline = this.isMainline,
            IsReleaseBranch = this.isReleaseBranch,
            LabelNumberPattern = this.labelNumberPattern,
            PreventIncrementOfMergedBranchVersion = this.preventIncrementOfMergedBranchVersion,
            PreReleaseWeight = this.preReleaseWeight
        };

        if (this.overrides.Any())
        {
            ConfigurationHelper configurationHelper = new(configuration);
            foreach (var item in this.overrides)
            {
                configurationHelper.Override(item);
            }
            configuration = configurationHelper.Configuration;
        }

        FinalizeConfiguration(configuration);
        ValidateConfiguration(configuration);

        return configuration;
    }

    private static void FinalizeConfiguration(IGitVersionConfiguration configuration)
    {
        foreach (var (name, branchConfiguration) in configuration.Branches)
        {
            FinalizeBranchConfiguration(configuration, name, branchConfiguration);
        }
    }

    private static void FinalizeBranchConfiguration(IGitVersionConfiguration configuration, string branchName,
        IBranchConfiguration branchConfiguration)
    {
        var branches = configuration.Branches;
        foreach (var targetBranchName in branchConfiguration.IsSourceBranchFor)
        {
            var targetBranchConfiguration = (BranchConfiguration)branches[targetBranchName];
            targetBranchConfiguration.SourceBranches.Add(branchName);
        }
    }

    private static void ValidateConfiguration(IGitVersionConfiguration configuration)
    {
        foreach (var (name, branchConfiguration) in configuration.Branches)
        {
            var helpUrl = $"{PathHelper.NewLine}See https://gitversion.net/docs/reference/configuration for more info";

            if (branchConfiguration.RegularExpression == null)
            {
                throw new ConfigurationException($"Branch configuration '{name}' is missing required configuration 'regex'{helpUrl}");
            }

            var sourceBranches = branchConfiguration.SourceBranches ?? throw new ConfigurationException($"Branch configuration '{name}' is missing required configuration 'source-branches'{helpUrl}");
            var missingSourceBranches = sourceBranches.Where(sb => !configuration.Branches.ContainsKey(sb)).ToArray();
            if (missingSourceBranches.Any())
            {
                throw new ConfigurationException($"Branch configuration '{name}' defines these 'source-branches' that are not configured: '[{string.Join(",", missingSourceBranches)}]'{helpUrl}");
            }
        }
    }

    protected record BranchMetaData
    {
        public string Name { get; init; }

        public string RegexPattern { get; init; }
    }
}
