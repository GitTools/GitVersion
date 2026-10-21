namespace GitVersion.VersionCalculation;

/// <summary>Registers the built-in <see cref="IVersionStrategy"/> implementations.</summary>
public class VersionStrategyModule : IGitVersionModule
{
    /// <summary>Registers the built-in version strategies as singletons.</summary>
    public void RegisterTypes(IServiceCollection services)
    {
        services.AddSingleton<IVersionStrategy, ConfiguredNextVersionVersionStrategy>();
        services.AddSingleton<IVersionStrategy, FallbackVersionStrategy>();
        services.AddSingleton<IVersionStrategy, MainlineVersionStrategy>();
        services.AddSingleton<IVersionStrategy, MergeMessageVersionStrategy>();
        services.AddSingleton<IVersionStrategy, TaggedCommitVersionStrategy>();
        services.AddSingleton<IVersionStrategy, TrackReleaseBranchesVersionStrategy>();
        services.AddSingleton<IVersionStrategy, VersionInBranchNameVersionStrategy>();
    }
}
