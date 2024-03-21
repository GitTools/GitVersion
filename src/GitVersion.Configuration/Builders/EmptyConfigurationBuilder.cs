using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

internal sealed class EmptyConfigurationBuilder : ConfigurationBuilderBase<EmptyConfigurationBuilder>
{
    public static EmptyConfigurationBuilder New => new();

    private EmptyConfigurationBuilder()
    {
        GitVersionConfiguration configuration = new()
        {
            DeploymentMode = DeploymentMode.ContinuousDelivery,
            AssemblyVersioningScheme = AssemblyVersioningScheme.MajorMinorPatch,
            AssemblyFileVersioningScheme = AssemblyFileVersioningScheme.MajorMinorPatch,
            CommitDateFormat = "yyyy-MM-dd",
            CommitMessageIncrementing = CommitMessageIncrementMode.Enabled,
            TagPreReleaseWeight = 0
        };
        WithConfiguration(configuration);
    }
}
