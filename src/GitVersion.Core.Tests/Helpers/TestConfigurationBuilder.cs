using GitVersion.Configuration;
using GitVersion.VersionCalculation;

namespace GitVersion.Core.Tests.Helpers;

// Please use GitFlowConfigurationBuilder or ScratchConfigurationBuilder class instead of this class.
internal sealed class TestConfigurationBuilder : TestConfigurationBuilderBase<TestConfigurationBuilder>
{
    public static TestConfigurationBuilder New => new();

    private TestConfigurationBuilder()
    {
        ConfigurationBuilder configurationBuilder = new();
        var configuration = configurationBuilder.Build();
        WithConfiguration(configuration);
    }

    public TestConfigurationBuilder WithPreventIncrementOfMergedBranchVersion(string branchName, bool? value)
    {
        WithBranch(branchName).WithPreventIncrementOfMergedBranchVersion(value);
        return this;
    }

    public TestConfigurationBuilder WithVersioningMode(string branchName, VersioningMode? value)
    {
        WithBranch(branchName).WithVersioningMode(value);
        return this;
    }

    public TestConfigurationBuilder WithoutTag(string branchName)
    {
        WithBranch(branchName).WithTag(null);
        return this;
    }

    public TestConfigurationBuilder WithTag(string branchName, string value)
    {
        WithBranch(branchName).WithTag(value);
        return this;
    }

    public TestConfigurationBuilder WithoutVersioningMode(string branchName)
    {
        WithBranch(branchName).WithVersioningMode(null);
        return this;
    }

    public TestConfigurationBuilder WithoutIncrement(string branchName)
    {
        WithBranch(branchName).WithIncrement(null);
        return this;
    }

    public TestConfigurationBuilder WithIncrement(string branchName, IncrementStrategy value)
    {
        WithBranch(branchName).WithIncrement(value);
        return this;
    }

    public TestConfigurationBuilder WithoutAnyTrackMergeTargets()
    {
        foreach (var item in base.branchConfigurationBuilders)
        {
            item.Value.WithTrackMergeTarget(false);
        }
        return this;
    }
}
