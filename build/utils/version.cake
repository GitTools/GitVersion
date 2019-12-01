public class BuildVersion
{
    public GitVersion GitVersion { get; private set; }
    public string Version { get; private set; }
    public string Milestone { get; private set; }
    public string SemVersion { get; private set; }
    public string GemVersion { get; private set; }
    public string NugetVersion { get; private set; }

    public static BuildVersion Calculate(ICakeContext context, BuildParameters parameters, GitVersion gitVersion)
    {
        var version = gitVersion.MajorMinorPatch;
        var semVersion = gitVersion.LegacySemVer;
        var nugetVersion = gitVersion.LegacySemVer;

        if (!string.IsNullOrWhiteSpace(gitVersion.BuildMetaData)) {
            semVersion += "-" + gitVersion.BuildMetaData;
            nugetVersion += "." + gitVersion.BuildMetaData;
        }

        return new BuildVersion
        {
            GitVersion   = gitVersion,
            Milestone    = version,
            Version      = version,
            SemVersion   = semVersion,
            GemVersion   = semVersion.Replace("-", "."),
            NugetVersion = nugetVersion.ToLowerInvariant(),
        };
    }
}
