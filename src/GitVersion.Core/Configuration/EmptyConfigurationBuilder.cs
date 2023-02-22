namespace GitVersion.Configuration;

internal sealed class EmptyConfigurationBuilder : ConfigurationBuilderBase<EmptyConfigurationBuilder>
{
    public static EmptyConfigurationBuilder New => new();

    private EmptyConfigurationBuilder()
    {
    }
}
