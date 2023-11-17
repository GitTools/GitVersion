namespace Common.Utilities;

public class Constants
{
    public const string RepoOwner = "GitTools";
    public const string Repository = "GitVersion";

    public const string Version60 = "6.0";
    public const string Version70 = "7.0";
    public const string Version80 = "8.0";
    public const string VersionLatest = Version80;

    public const string NetVersion60 = $"net{Version60}";
    public const string NetVersion70 = $"net{Version70}";
    public const string NetVersion80 = $"net{Version80}";
    public const string NetVersionLatest = $"net{VersionLatest}";

    public const string DefaultBranch = "main";
    public const string DefaultConfiguration = "Release";

    public static readonly Architecture[] ArchToBuild = { Architecture.Amd64, Architecture.Arm64 };
    public static readonly string[] VersionsToBuild = { Version60, Version70, Version80 };
    public static readonly string[] Frameworks = { NetVersion60, NetVersion70, NetVersion80 };

    public static readonly string[] DistrosToSkipForArtifacts = Array.Empty<string>();
    public static readonly string[] DistrosToSkipForDocker = Array.Empty<string>();

    public const string DockerBaseImageName = "gittools/build-images";
    public const string DockerImageName = "gittools/gitversion";

    public const string DockerHub = "dockerhub";
    public const string GitHub = "github";
    public const string DockerHubRegistry = "docker.io";
    public const string GitHubContainerRegistry = "ghcr.io";

    public const string Arm64 = "arm64";
    public const string Amd64 = "amd64";

    public const string Alpine317 = "alpine.3.17";
    public const string Alpine318 = "alpine.3.18";
    public const string CentosStream8 = "centos.stream.8";
    public const string Debian11 = "debian.11";
    public const string Fedora37 = "fedora.37";
    public const string Ubuntu2004 = "ubuntu.20.04";
    public const string Ubuntu2204 = "ubuntu.22.04";
    public const string DockerDistroLatest = Debian11;
    public const string DebianLatest = Debian11;
    public const string UbuntuLatest = Ubuntu2204;
    public const string AlpineLatest = Alpine318;

    public static readonly string[] DockerDistrosToBuild =
    {
        Alpine317,
        Alpine318,
        CentosStream8,
        Debian11,
        Fedora37,
        Ubuntu2004,
        Ubuntu2204
    };
    public const string NugetOrgUrl = "https://api.nuget.org/v3/index.json";
    public const string GithubPackagesUrl = "https://nuget.pkg.github.com/gittools/index.json";
    public const string ChocolateyUrl = "https://push.chocolatey.org/";
}
