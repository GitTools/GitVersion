#load "./paths.cake"
#load "./artifacts.cake"
#load "./credentials.cake"
#load "./version.cake"

public class BuildParameters
{
    public string Target { get; private set; }
    public string Configuration { get; private set; }

    public const string MainRepoOwner = "gittools";
    public const string MainRepoName = "GitVersion";
    public string CoreFxVersion21 { get; private set; }  = "netcoreapp2.1";
    public string CoreFxVersion31 { get; private set; }  = "netcoreapp3.1";
    public string FullFxVersion472 { get; private set; } = "net472";

    public string DockerDistro { get; private set; }
    public string DockerDotnetVersion { get; private set; }

    public bool EnabledUnitTests { get; private set; }
    public bool EnabledPublishGem { get; private set; }
    public bool EnabledPublishNuget { get; private set; }
    public bool EnabledPublishChocolatey { get; private set; }
    public bool EnabledPublishDocker { get; private set; }

    public bool IsRunningOnUnix { get; private set; }
    public bool IsRunningOnWindows { get; private set; }
    public bool IsRunningOnLinux { get; private set; }
    public bool IsRunningOnMacOS { get; private set; }

    public bool IsDockerForWindows { get; private set; }
    public bool IsDockerForLinux { get; private set; }
    public string DockerRootPrefix { get; private set; }

    public bool IsLocalBuild { get; private set; }
    public bool IsRunningOnAppVeyor { get; private set; }
    public bool IsRunningOnTravis { get; private set; }
    public bool IsRunningOnAzurePipeline { get; private set; }

    public bool IsMainRepo { get; private set; }
    public bool IsMainBranch { get; private set; }
    public bool IsTagged { get; private set; }
    public bool IsPullRequest { get; private set; }

    public DotNetCoreMSBuildSettings MSBuildSettings { get; private set; }

    public BuildCredentials Credentials { get; private set; }
    public BuildVersion Version { get; private set; }
    public BuildPaths Paths { get; private set; }
    public BuildPackages Packages { get; private set; }
    public BuildArtifacts Artifacts { get; private set; }
    public DockerImages Docker { get; private set; }
    public Dictionary<string, DirectoryPath> PackagesBuildMap { get; private set; }
    public Dictionary<PlatformFamily, string> NativeRuntimes { get; private set; }

    public bool IsStableRelease() => !IsLocalBuild && IsMainRepo && IsMainBranch && !IsPullRequest && IsTagged;
    public bool IsPreRelease()    => !IsLocalBuild && IsMainRepo && IsMainBranch && !IsPullRequest && !IsTagged;

    public bool CanPostToGitter => !string.IsNullOrWhiteSpace(Credentials.Gitter.Token) && !string.IsNullOrWhiteSpace(Credentials.Gitter.RoomId);

    public static BuildParameters GetParameters(ICakeContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var target = context.Argument("target", "Default");
        var buildSystem = context.BuildSystem();

        var dockerCliPlatform = ((buildSystem.IsRunningOnAzurePipelines || buildSystem.IsRunningOnAzurePipelinesHosted)
                                && context.Environment.Platform.Family != PlatformFamily.OSX)
                                || buildSystem.IsLocalBuild
                                ? context.GetDockerCliPlatform() : "";

        return new BuildParameters {
            Target        = target,
            Configuration = context.Argument("configuration", "Release"),

            DockerDistro        = context.Argument("docker_distro", ""),
            DockerDotnetVersion = context.Argument("docker_dotnetversion", ""),

            EnabledUnitTests          = context.IsEnabled("ENABLED_UNIT_TESTS"),
            EnabledPublishGem         = context.IsEnabled("ENABLED_PUBLISH_GEM"),
            EnabledPublishNuget       = context.IsEnabled("ENABLED_PUBLISH_NUGET"),
            EnabledPublishChocolatey  = context.IsEnabled("ENABLED_PUBLISH_CHOCOLATEY"),
            EnabledPublishDocker      = context.IsEnabled("ENABLED_PUBLISH_DOCKER"),

            IsRunningOnUnix    = context.IsRunningOnUnix(),
            IsRunningOnWindows = context.IsRunningOnWindows(),
            IsRunningOnLinux   = context.Environment.Platform.Family == PlatformFamily.Linux,
            IsRunningOnMacOS   = context.Environment.Platform.Family == PlatformFamily.OSX,

            IsLocalBuild             = buildSystem.IsLocalBuild,
            IsRunningOnAppVeyor      = buildSystem.IsRunningOnAppVeyor,
            IsRunningOnTravis        = buildSystem.IsRunningOnTravisCI,
            IsRunningOnAzurePipeline = buildSystem.IsRunningOnAzurePipelines || buildSystem.IsRunningOnAzurePipelinesHosted,

            IsDockerForWindows = dockerCliPlatform == "windows",
            IsDockerForLinux   = dockerCliPlatform == "linux",
            DockerRootPrefix   = dockerCliPlatform == "windows" ? "c:" : "",

            IsPullRequest = buildSystem.IsPullRequest,
            IsMainRepo    = context.IsOnMainRepo(),
            IsMainBranch  = context.IsOnMainBranch(),
            IsTagged      = context.IsBuildTagged(),

            MSBuildSettings = GetMsBuildSettings(context)
        };
    }

    public void Initialize(ICakeContext context, GitVersion gitVersion)
    {
        Version = BuildVersion.Calculate(context, this, gitVersion);

        Paths = BuildPaths.GetPaths(context, this, Configuration, Version);

        Docker = DockerImages.GetDockerImages(context, this);

        Packages = BuildPackages.GetPackages(
            Paths.Directories.NugetRoot,
            Version,
            new [] { "GitVersion.CommandLine", "GitVersionTask", "GitVersion.Tool" },
            new [] { "GitVersion.Portable" });

        var files = Paths.Files;

        var buildArtifacts = context.GetFiles(Paths.Directories.BuildArtifact + "/*.*") + context.GetFiles(Paths.Directories.Artifacts + "/*.tar.gz");
        buildArtifacts += files.ReleaseNotesOutputFilePath;

        Artifacts = BuildArtifacts.GetArtifacts(buildArtifacts.ToArray());

        PackagesBuildMap = new Dictionary<string, DirectoryPath>
        {
            ["GitVersion.CommandLine"] = Paths.Directories.ArtifactsBinCmdline,
            ["GitVersion.Portable"] = Paths.Directories.ArtifactsBinPortable,
        };

        NativeRuntimes = new Dictionary<PlatformFamily, string>
        {
            [PlatformFamily.Windows] = "win-x64",
            [PlatformFamily.Linux]   = "linux-x64",
            [PlatformFamily.OSX]     = "osx-x64",
        };

        Credentials = BuildCredentials.GetCredentials(context);
        SetMSBuildSettingsVersion(MSBuildSettings, Version);
    }

    private static DotNetCoreMSBuildSettings GetMsBuildSettings(ICakeContext context)
    {
        var msBuildSettings = new DotNetCoreMSBuildSettings();
        if(!context.IsRunningOnWindows())
        {
            var frameworkPathOverride = context.Environment.Runtime.IsCoreClr
                                        ?   new []{
                                                new DirectoryPath("/Library/Frameworks/Mono.framework/Versions/Current/lib/mono"),
                                                new DirectoryPath("/usr/lib/mono"),
                                                new DirectoryPath("/usr/local/lib/mono")
                                            }
                                            .Select(directory =>directory.Combine("4.5"))
                                            .FirstOrDefault(directory => context.DirectoryExists(directory))
                                            ?.FullPath + "/"
                                        : new FilePath(typeof(object).Assembly.Location).GetDirectory().FullPath + "/";

            // Use FrameworkPathOverride when not running on Windows.
            context.Information("Build will use FrameworkPathOverride={0} since not building on Windows.", frameworkPathOverride);
            msBuildSettings.WithProperty("FrameworkPathOverride", frameworkPathOverride);
            msBuildSettings.WithProperty("POSIX", "true");
        }

        return msBuildSettings;
    }

    private void SetMSBuildSettingsVersion(DotNetCoreMSBuildSettings msBuildSettings, BuildVersion version)
    {
        msBuildSettings.WithProperty("Version", version.SemVersion);
        msBuildSettings.WithProperty("AssemblyVersion", version.Version);
        msBuildSettings.WithProperty("PackageVersion", version.NugetVersion);
        msBuildSettings.WithProperty("FileVersion", version.Version);
        msBuildSettings.WithProperty("NoPackageAnalysis", "true");
    }
}
