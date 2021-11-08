using Build.Utilities;
using Cake.Common.Tools.DotNetCore.MSBuild;
using Common.Utilities;

namespace Build;

public class BuildContext : BuildContextBase
{
    public string MsBuildConfiguration { get; set; } = "Release";

    public readonly Dictionary<PlatformFamily, string[]> NativeRuntimes = new()
    {
        [PlatformFamily.Windows] = new[] { "win-x64", "win-x86" },
        [PlatformFamily.Linux] = new[] { "linux-x64", "linux-musl-x64", "linux-arm64" },
        [PlatformFamily.OSX] = new[] { "osx-x64" },
    };

    public bool EnabledUnitTests { get; set; }

    public Credentials? Credentials { get; set; }

    // TODO update to DotNetMSBuildSettings when available
    public DotNetCoreMSBuildSettings MsBuildSettings { get; } = new();

    public BuildContext(ICakeContext context) : base(context)
    {
    }
}
