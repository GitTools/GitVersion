using Build.Utilities;
using Common.Utilities;

namespace Build;

public class BuildContext : BuildContextBase
{
    public string MsBuildConfiguration { get; set; } = "Release";

    public readonly Dictionary<PlatformFamily, string[]> NativeRuntimes = new()
    {
        [PlatformFamily.Windows] = new[] { "win-x64", "win-x86", "win-arm64" },
        [PlatformFamily.Linux] = new[] { "linux-x64", "linux-musl-x64", "linux-arm64", "linux-musl-arm64" },
        [PlatformFamily.OSX] = new[] { "osx-x64", "osx-arm64" },
    };

    public bool EnabledUnitTests { get; set; }

    public Credentials? Credentials { get; set; }

    public DotNetMSBuildSettings MsBuildSettings { get; } = new();

    public BuildContext(ICakeContext context) : base(context)
    {
    }
}
