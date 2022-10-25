namespace GitVersion.Core.Tests.Helpers;

internal sealed class EmptyConfigurationBuilder : TestConfigurationBuilderBase<EmptyConfigurationBuilder>
{
    public static EmptyConfigurationBuilder New => new();

    private EmptyConfigurationBuilder()
    {
    }
}
