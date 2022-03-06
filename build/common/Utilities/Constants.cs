namespace Common.Utilities;

public class Constants
{
    public const string RepoOwner = "GitTools";
    public const string Repository = "GitVersion";

    public const string Version60 = "6.0";
    public const string Version31 = "3.1";

    public const string NetVersion60 = "net6.0";
    public const string CoreFxVersion31 = "netcoreapp3.1";
    public const string FullFxVersion48 = "net48";

    public const string NoMono = "NoMono";
    public const string NoNet48 = "NoNet48";

    public static readonly string[] VersionsToBuild = { Version60, Version31 };
    public static readonly Architecture[] ArchToBuild = { Architecture.Amd64, Architecture.Arm64 };
    public static readonly string[] DistrosToSkip = { Alpine312, Alpine313, Alpine314, Centos7 };

    public const string DockerBaseImageName = "gittools/build-images";
    public const string DockerImageName = "gittools/gitversion";

    public const string DockerHub = "dockerhub";
    public const string GitHub = "github";
    public const string DockerHubRegistry = "docker.io";
    public const string GitHubContainerRegistry = "ghcr.io";

    public const string Arm64 = "arm64";
    public const string Amd64 = "amd64";

    public const string Alpine312 = "alpine.3.12";
    public const string Alpine313 = "alpine.3.13";
    public const string Alpine314 = "alpine.3.14";
    public const string Centos7 = "centos.7";
    public const string Centos8 = "centos.8";
    public const string Debian9 = "debian.9";
    public const string Debian10 = "debian.10";
    public const string Debian11 = "debian.11";
    public const string Fedora33 = "fedora.33";
    public const string Ubuntu1804 = "ubuntu.18.04";
    public const string Ubuntu2004 = "ubuntu.20.04";
    public const string DockerDistroLatest = Debian10;
    public static readonly string[] DockerDistrosToBuild =
    {
        Alpine312,
        Alpine313,
        Alpine314,
        Centos7,
        Centos8,
        Debian9,
        Debian10,
        Debian11,
        Fedora33,
        Ubuntu1804,
        Ubuntu2004
    };
    public const string NugetOrgUrl = "https://api.nuget.org/v3/index.json";
    public const string GithubPackagesUrl = "https://nuget.pkg.github.com/gittools/index.json";
    public const string ChocolateyUrl = "https://push.chocolatey.org/";
}
