using Common.Addins.GitVersion;

namespace Common.Utilities
{
    public class BuildVersion
    {
        public GitVersion? GitVersion { get; init; }
        public string? Version { get; init; }
        public string? Milestone { get; init; }
        public string? SemVersion { get; init; }
        public string? NugetVersion { get; init; }

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

            return new BuildVersion
            {
                GitVersion = gitVersion,
                Version = version,
                Milestone = version,
                SemVersion = semVersion,
                NugetVersion = nugetVersion?.ToLowerInvariant()
            };
        }
    }
}
