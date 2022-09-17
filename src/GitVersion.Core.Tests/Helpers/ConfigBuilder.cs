using GitVersion.Configuration;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.Helpers;

public sealed class ConfigBuilder
{
    public static ConfigBuilder New => new();

    private string? nextVerson;
    private VersioningMode versioningMode;
    private bool withoutAnyTrackMergeTargets;
    private readonly Dictionary<string, bool> trackMergeTargetsDictionary = new();
    private readonly Dictionary<string, bool> preventIncrementOfMergedBranchVersionDictionary = new();
    private IncrementStrategy? increment;
    private readonly Dictionary<string, IncrementStrategy> incrementDictionary = new();
    private IgnoreConfig? ignoreConfig;

    private ConfigBuilder()
    {
        withoutAnyTrackMergeTargets = false;
        increment = IncrementStrategy.Inherit;
        versioningMode = VersioningMode.ContinuousDelivery;
    }

    public ConfigBuilder WithNextVersion(string? value)
    {
        nextVerson = value;
        return this;
    }

    public ConfigBuilder WithVersioningMode(VersioningMode value)
    {
        versioningMode = value;
        return this;
    }

    public ConfigBuilder WithTrackMergeTarget(string branch, bool value)
    {
        trackMergeTargetsDictionary[branch] = value;
        return this;
    }

    public ConfigBuilder WithoutAnyTrackMergeTargets()
    {
        withoutAnyTrackMergeTargets = true;
        trackMergeTargetsDictionary.Clear();
        return this;
    }

    public ConfigBuilder WithPreventIncrementOfMergedBranchVersion(string branch, bool value)
    {
        preventIncrementOfMergedBranchVersionDictionary[branch] = value;
        return this;
    }

    public ConfigBuilder WithIncrement(IncrementStrategy? value)
    {
        increment = value;
        return this;
    }

    public ConfigBuilder WithIncrement(string branch, IncrementStrategy value)
    {
        incrementDictionary[branch] = value;
        return this;
    }

    public ConfigBuilder WithIgnoreConfig(IgnoreConfig value)
    {
        ignoreConfig = value;
        return this;
    }

    public Config Build()
    {
        Config configuration = new()
        {
            NextVersion = nextVerson,
            VersioningMode = versioningMode
        };

        if (ignoreConfig != null)
        {
            configuration.Ignore = ignoreConfig;
        }

        ConfigurationBuilder configurationBuilder = new();
        configuration = configurationBuilder.Add(configuration).Build();

        if (withoutAnyTrackMergeTargets)
        {
            foreach (var branchConfiguration in configuration.Branches.Values)
            {
                branchConfiguration.TrackMergeTarget = false;
            }
        }

        foreach (var item in trackMergeTargetsDictionary)
        {
            configuration.Branches[item.Key].TrackMergeTarget = item.Value;
        }

        foreach (var item in preventIncrementOfMergedBranchVersionDictionary)
        {
            configuration.Branches[item.Key].PreventIncrementOfMergedBranchVersion = item.Value;
        }

        configuration.Increment = increment;

        foreach (var item in incrementDictionary)
        {
            configuration.Branches[item.Key].Increment = item.Value;
        }

        return configuration;
    }
}
