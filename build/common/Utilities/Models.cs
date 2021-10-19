using Common.Addins.GitVersion;

namespace Common.Utilities;

public record NugetPackage(string PackageName, FilePath FilePath, bool IsChocoPackage);

public record CodeCovCredentials(string Token);

public record GitHubCredentials(string Token, string? UserName = null);

public record NugetCredentials(string ApiKey);

public record ChocolateyCredentials(string ApiKey);

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

public record DockerImage(string Distro, string TargetFramework, Architecture Architecture, string Registry, bool UseBaseImage);

public enum DockerRegistry
{
    GitHub = 0,
    DockerHub = 1
}
