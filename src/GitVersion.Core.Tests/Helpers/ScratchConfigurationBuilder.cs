namespace GitVersion.Core.Tests.Helpers;

internal sealed class ScratchConfigurationBuilder : TestConfigurationBuilderBase<ScratchConfigurationBuilder>
{
    public static ScratchConfigurationBuilder New => new();

    private ScratchConfigurationBuilder()
    {
    }
}
