using Common.Addins.GitVersion;

namespace Common.Utilities
{
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
