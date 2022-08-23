using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.Options;

namespace GitVersion;

// TODO 3074: test
internal sealed class IgnoredFilterProvider : IIgnoredFilterProvider
{
    // TODO 3074: adjust naming, order, whitespaces etc.
    public IConfigProvider ConfigProvider { get; }

    public IOptions<GitVersionOptions> Options { get; }

    public IgnoredFilterProvider(IConfigProvider configProvider, IOptions<GitVersionOptions> options)
    {
        ConfigProvider = configProvider.NotNull();
        Options = options.NotNull();
    }

    public IVersionFilter[] Provide() =>
        this.ConfigProvider.Provide(Options.Value.ConfigInfo.OverrideConfig)
            .Ignore.ToFilters().ToArray();
}
