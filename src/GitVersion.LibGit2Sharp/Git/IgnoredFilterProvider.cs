using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.Options;

namespace GitVersion;

// TODO 3074: test
internal sealed class IgnoredFilterProvider : IIgnoredFilterProvider
{
    private readonly IConfigProvider configProvider;
    private readonly IOptions<GitVersionOptions> options;

    public IgnoredFilterProvider(IConfigProvider configProvider, IOptions<GitVersionOptions> options)
    {
        this.configProvider = configProvider.NotNull();
        this.options = options.NotNull();
    }

    public IVersionFilter[] Provide() =>
        this.configProvider.Provide(options.Value.ConfigInfo.OverrideConfig)
            .Ignore.ToFilters().ToArray();
}
