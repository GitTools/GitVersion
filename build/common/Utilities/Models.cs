using Cake.Incubator.AssertExtensions;
using Common.Addins.GitVersion;

namespace Common.Utilities;

public record NugetPackage(string PackageName, FilePath FilePath, bool IsChocoPackage);

public record CodeCovCredentials(string Token);

public record GitHubCredentials(string Token, string? UserName = null);

public record NugetCredentials(string ApiKey);

public record ChocolateyCredentials(string ApiKey);

public record BuildVersion(GitVersion GitVersion, string? Version, string? Milestone, string? SemVersion, string? NugetVersion, string? ChocolateyVersion, bool IsPreRelease)
{
    public static BuildVersion Calculate(GitVersion gitVersion)
    {
        var version = gitVersion.MajorMinorPatch;
        var semVersion = gitVersion.SemVer;
        var nugetVersion = gitVersion.SemVer;
        var chocolateyVersion = gitVersion.MajorMinorPatch;

        if (!string.IsNullOrWhiteSpace(gitVersion.PreReleaseTag))
        {
            // Chocolatey does not support pre-release tags with dots, so we replace them with dashes
            // if the pre-release tag is a number, we add a "a" prefix to the pre-release tag
            // the trick should be removed when Chocolatey supports semver 2.0
            var prefix = int.TryParse(gitVersion.PreReleaseLabel, out _) ? "a" : string.Empty;
            chocolateyVersion += $"-{prefix}{gitVersion.PreReleaseTag?.Replace(".", "-")}";
        }

        if (!string.IsNullOrWhiteSpace(gitVersion.BuildMetaData))
        {
            semVersion += $"-{gitVersion.BuildMetaData}";
            chocolateyVersion += $"-{gitVersion.BuildMetaData}";
            nugetVersion += $".{gitVersion.BuildMetaData}";
        }

        return new BuildVersion(
            GitVersion: gitVersion,
            Version: version,
            Milestone: version,
            SemVersion: semVersion,
            NugetVersion: nugetVersion?.ToLowerInvariant(),
            ChocolateyVersion: chocolateyVersion?.ToLowerInvariant(),
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
