namespace Common.Utilities
{
    public class Constants
    {
        public const string NetVersion50 = "net5.0";
        public const string CoreFxVersion31 = "netcoreapp3.1";
        public const string FullFxVersion48 = "net48";

        public static readonly string DockerHubRegistry = $"docker.io/{DockerImageName}";
        public static readonly string GitHubContainerRegistry = $"ghcr.io/{DockerImageName}";
        private const string DockerImageName = "gittools/build-images";
    }
}
