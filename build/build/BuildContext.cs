using Build.Utilities;
using Common.Utilities;

namespace Build;

public class BuildContext(ICakeContext context) : BuildContextBase(context)
{
    public string MsBuildConfiguration { get; set; } = Constants.DefaultConfiguration;

    public readonly Dictionary<PlatformFamily, string[]> NativeRuntimes = new()
    {
        [PlatformFamily.Windows] = ["win-x64", "win-arm64"],
        [PlatformFamily.Linux] = ["linux-x64", "linux-musl-x64", "linux-arm64", "linux-musl-arm64"],
        [PlatformFamily.OSX] = ["osx-x64", "osx-arm64"],
    };

    public bool EnabledUnitTests { get; set; }

    public Credentials? Credentials { get; set; }

    public DotNetMSBuildSettings MsBuildSettings { get; } = new();
}
