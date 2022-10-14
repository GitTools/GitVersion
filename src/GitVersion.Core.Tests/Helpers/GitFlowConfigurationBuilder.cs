using GitVersion.Configuration;

namespace GitVersion.Core.Tests.Helpers;

internal sealed class GitFlowConfigurationBuilder : TestConfigurationBuilderBase<GitFlowConfigurationBuilder>
{
    public static GitFlowConfigurationBuilder New => new();

    private GitFlowConfigurationBuilder()
    {
        ConfigurationBuilder configurationBuilder = new();
        var configuration = configurationBuilder.Build();
        WithConfiguration(configuration);
    }
}
