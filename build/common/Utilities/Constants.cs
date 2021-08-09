namespace Common.Utilities
{
    public class Constants
    {
        public const string RepoOwner = "gittools";
        public const string Repository = "GitVersion";

        public const string Version50 = "5.0";
        public const string Version31 = "3.1";

        public const string NetVersion50 = "net5.0";
        public const string CoreFxVersion31 = "netcoreapp3.1";
        public const string FullFxVersion48 = "net48";
        public static readonly string[] VersionsToBuild = { Version50, Version31 };

        public const string DockerDistroLatest = "debian.10-x64";
        private const string DockerImageName = "gittools/build-images";

        public static readonly string DockerHubRegistry = $"docker.io/{DockerImageName}";
        public static readonly string GitHubContainerRegistry = $"ghcr.io/{DockerImageName}";
        public static readonly string[] DockerDistrosToBuild =
        {
            "alpine.3.12-x64",
            "centos.7-x64",
            "centos.8-x64",
            "debian.9-x64",
            "debian.10-x64",
            "fedora.33-x64",
            "ubuntu.16.04-x64",
            "ubuntu.18.04-x64",
            "ubuntu.20.04-x64"
        };
        public const string NugetOrgUrl = "https://api.nuget.org/v3/index.json";
        public const string GithubPackagesUrl = "https://nuget.pkg.github.com/gittools/index.json";
        public const string ChocolateyUrl = "https://push.chocolatey.org/";
    }
}
