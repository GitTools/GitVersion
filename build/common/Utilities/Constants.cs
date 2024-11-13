// ReSharper disable MemberCanBePrivate.Global
namespace Common.Utilities;

public static class Constants
{
    public const string RepoOwner = "GitTools";
    public const string Repository = "GitVersion";

    public const string VersionLatest = "8.0";
    public static readonly string[] DotnetVersions = [VersionLatest, "9.0"];

    public const string DefaultBranch = "main";
    public const string DefaultConfiguration = "Release";

    public static readonly Architecture[] ArchToBuild = [Architecture.Amd64, Architecture.Arm64];
    public static readonly string[] Architectures = [nameof(Architecture.Amd64), nameof(Architecture.Arm64)];

    public const string DockerBaseImageName = "gittools/build-images";
    public const string DockerImageName = "gittools/gitversion";

    public const string DockerHub = "dockerhub";
    public const string GitHub = "github";
    public const string DockerHubRegistry = "docker.io";
    public const string GitHubContainerRegistry = "ghcr.io";
    public static readonly string[] DockerRegistries = [DockerHub, GitHub];

    public const string AlpineLatest = "alpine.3.20";
    public const string CentosStreamLatest = "centos.stream.9";
    public const string DebianLatest = "debian.12";
    public const string FedoraLatest = "fedora.40";
    public const string Ubuntu2004 = "ubuntu.20.04";
    public const string Ubuntu2204 = "ubuntu.22.04";
    public const string Ubuntu2404 = "ubuntu.24.04";

    public const string DockerDistroLatest = DebianLatest;

    public static readonly string[] DockerDistros =
    [
        AlpineLatest,
        CentosStreamLatest,
        DebianLatest,
        FedoraLatest,
        Ubuntu2004,
        Ubuntu2204,
        Ubuntu2404
    ];
    public const string NugetOrgUrl = "https://api.nuget.org/v3/index.json";
    public const string GithubPackagesUrl = "https://nuget.pkg.github.com/gittools/index.json";
    public const string ChocolateyUrl = "https://push.chocolatey.org/";
}
