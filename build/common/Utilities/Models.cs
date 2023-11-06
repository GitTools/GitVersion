using Cake.Incubator.AssertExtensions;
using Common.Addins.GitVersion;

namespace Common.Utilities;

public record NugetPackage(string PackageName, FilePath FilePath, bool IsChocoPackage);

public record CodeCovCredentials(string Token);

public record GitHubCredentials(string Token, string? UserName = null);

public record NugetCredentials(string ApiKey);

public record DockerHubCredentials(string Username, string Password);

public record ChocolateyCredentials(string ApiKey);

public record BuildVersion(GitVersion GitVersion, string? Version, string? Milestone, string? SemVersion, string? NugetVersion, bool IsPreRelease)
{
    public static BuildVersion Calculate(GitVersion gitVersion)
    {
        var version = gitVersion.MajorMinorPatch;
        var semVersion = gitVersion.SemVer;
        var nugetVersion = gitVersion.SemVer;

        if (!string.IsNullOrWhiteSpace(gitVersion.BuildMetaData))
        {
            semVersion += $"-{gitVersion.BuildMetaData}";
            nugetVersion += $".{gitVersion.BuildMetaData}";
        }

        return new BuildVersion(
            GitVersion: gitVersion,
            Version: version,
            Milestone: semVersion,
            SemVersion: semVersion,
            NugetVersion: nugetVersion?.ToLowerInvariant(),
            IsPreRelease: !gitVersion.PreReleaseLabel.IsNullOrEmpty()
        );
    }
}

public record DockerImage(string Distro, string TargetFramework, Architecture Architecture, string Registry, bool UseBaseImage);

public enum DockerRegistry
{
    GitHub = 0,
    DockerHub = 1
}
