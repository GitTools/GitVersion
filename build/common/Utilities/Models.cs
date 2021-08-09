using Cake.Core.IO;
using Common.Addins.GitVersion;

namespace Common.Utilities
{
    public record NugetPackage(string PackageName, FilePath FilePath, bool IsChocoPackage);

    public record DockerImage(string Distro, string TargetFramework);

    public record CodeCovCredentials(string Token);

    public record GitHubCredentials(string UserName, string Token);

    public record GitterCredentials(string Token, string RoomId);

    public record DockerCredentials(string UserName, string Password);

    public record NugetCredentials(string ApiKey, string ApiUrl);

    public record ChocolateyCredentials(string ApiKey, string ApiUrl);

    public record BuildVersion(GitVersion GitVersion, string? Version, string? Milestone, string? SemVersion, string? NugetVersion)
    {
        public static BuildVersion Calculate(GitVersion gitVersion)
        {
            var version = gitVersion.MajorMinorPatch;
            var semVersion = gitVersion.LegacySemVer;
            var nugetVersion = gitVersion.LegacySemVer;

            if (!string.IsNullOrWhiteSpace(gitVersion.BuildMetaData))
            {
                semVersion += $"-{gitVersion.BuildMetaData}";
                nugetVersion += $".{gitVersion.BuildMetaData}";
            }

            return new BuildVersion(
                GitVersion: gitVersion,
                Version: version,
                Milestone: version,
                SemVersion: semVersion,
                NugetVersion: nugetVersion?.ToLowerInvariant()
            );
        }
    }
}
