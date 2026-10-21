using Build.Utilities;
using Common.Utilities;

namespace Build;

public class BuildContext(ICakeContext context) : BuildContextBase(context)
{
    public string MsBuildConfiguration { get; set; } = Constants.DefaultConfiguration;

    public readonly IReadOnlyDictionary<PlatformFamily, string[]> NativeRuntimes = new Dictionary<PlatformFamily, string[]>()
    {
        [PlatformFamily.Windows] = ["win-x64", "win-arm64"],
        [PlatformFamily.Linux] = ["linux-x64", "linux-musl-x64", "linux-arm64", "linux-musl-arm64"],
        [PlatformFamily.OSX] = ["osx-arm64"],
    };

    public bool EnabledUnitTests { get; set; }

    // Linux releases are built in native glibc/musl containers on each CPU architecture.
    // NativeAOT cannot use the former cross-OS/ABI publication loop.
    public string[] PackageRuntimes => IsOnLinux
        ? [System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier]
        : this.NativeRuntimes[Platform];

    public Credentials? Credentials { get; set; }

    public DotNetMSBuildSettings MsBuildSettings { get; } = new();
}
