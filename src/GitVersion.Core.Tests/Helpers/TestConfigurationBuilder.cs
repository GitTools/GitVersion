using GitVersion.Configuration;
using GitVersion.Model.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.Helpers;

public sealed class TestConfigurationBuilder
{
    public static TestConfigurationBuilder New => new();

    private string? nextVerson;
    private VersioningMode? versioningMode;
    private readonly Dictionary<string, VersioningMode?> versioningModeDictionary = new();
    private bool withoutAnyTrackMergeTargets;
    private readonly Dictionary<string, bool> trackMergeTargetsDictionary = new();
    private readonly Dictionary<string, bool> preventIncrementOfMergedBranchVersionDictionary = new();
    private readonly Dictionary<string, bool> preventIncrementOfTrackedReleaseBranchVersionDictionary = new();
    private IncrementStrategy? increment;
    private readonly Dictionary<string, IncrementStrategy> incrementDictionary = new();
    private readonly Dictionary<string, string?> tagDictionary = new();
    private IgnoreConfig? ignoreConfig;

    private TestConfigurationBuilder()
    {
        withoutAnyTrackMergeTargets = false;
        increment = IncrementStrategy.Inherit;
        versioningMode = VersioningMode.ContinuousDelivery;
    }

    public TestConfigurationBuilder WithNextVersion(string? value)
    {
        nextVerson = value;
        return this;
    }

    public TestConfigurationBuilder WithVersioningMode(VersioningMode value)
    {
        versioningMode = value;
        return this;
    }

    public TestConfigurationBuilder WithoutVersioningMode()
    {
        versioningMode = null;
        return this;
    }

    public TestConfigurationBuilder WithVersioningMode(string branch, VersioningMode value)
    {
        versioningModeDictionary[branch] = value;
        return this;
    }

    public TestConfigurationBuilder WithoutVersioningMode(string branch)
    {
        versioningModeDictionary[branch] = null;
        return this;
    }

    public TestConfigurationBuilder WithTrackMergeTarget(string branch, bool value)
    {
        trackMergeTargetsDictionary[branch] = value;
        return this;
    }

    public TestConfigurationBuilder WithoutAnyTrackMergeTargets()
    {
        withoutAnyTrackMergeTargets = true;
        trackMergeTargetsDictionary.Clear();
        return this;
    }

    public TestConfigurationBuilder WithPreventIncrementOfMergedBranchVersion(string branch, bool value)
    {
        preventIncrementOfMergedBranchVersionDictionary[branch] = value;
        return this;
    }

    public TestConfigurationBuilder WithPreventIncrementOfTrackedReleaseBranchVersion(string branch, bool value)
    {
        preventIncrementOfTrackedReleaseBranchVersionDictionary[branch] = value;
        return this;
    }

    public TestConfigurationBuilder WithIncrement(IncrementStrategy? value)
    {
        increment = value;
        return this;
    }

    public TestConfigurationBuilder WithIncrement(string branch, IncrementStrategy value)
    {
        incrementDictionary[branch] = value;
        return this;
    }

    public TestConfigurationBuilder WithoutTag(string branch)
    {
        tagDictionary[branch] = null;
        return this;
    }

    public TestConfigurationBuilder WithTag(string branch, string value)
    {
        tagDictionary[branch] = value;
        return this;
    }

    public TestConfigurationBuilder WithIgnoreConfig(IgnoreConfig value)
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

        foreach (var item in versioningModeDictionary)
        {
            configuration.Branches[item.Key].VersioningMode = item.Value;
        }

        foreach (var item in preventIncrementOfMergedBranchVersionDictionary)
        {
            configuration.Branches[item.Key].PreventIncrementOfMergedBranchVersion = item.Value;
        }

        foreach (var item in preventIncrementOfTrackedReleaseBranchVersionDictionary)
        {
            configuration.Branches[item.Key].PreventIncrementOfTrackedReleaseBranchVersion = item.Value;
        }

        configuration.Increment = increment;

        foreach (var item in incrementDictionary)
        {
            configuration.Branches[item.Key].Increment = item.Value;
        }

        foreach (var item in tagDictionary)
        {
            configuration.Branches[item.Key].Tag = item.Value;
        }

        return configuration;
    }
}
