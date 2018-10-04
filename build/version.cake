public class BuildVersion
{
    public string Version { get; private set; }
    public string SemVersion { get; private set; }
    public string NuGetVersion { get; private set; }
    public string DotNetAsterix { get; private set; }
    public string DotNetVersion { get; private set; }
    public string PreReleaseTag { get; private set; }

    public static BuildVersion Calculate(ICakeContext context, BuildParameters parameters, GitVersion gitVersion)
    {
        var version = gitVersion.MajorMinorPatch;
        var preReleaseTag = gitVersion.PreReleaseTag;
        var semVersion = gitVersion.LegacySemVerPadded;
        var dotnetVersion = version;

        if (!string.IsNullOrWhiteSpace(gitVersion.BuildMetaDataPadded)) {
            semVersion += "." + gitVersion.BuildMetaDataPadded;
            dotnetVersion += "." + gitVersion.BuildMetaDataPadded;
        }

        return new BuildVersion
        {
            Version       = version,
            SemVersion    = semVersion,
            NuGetVersion  = gitVersion.NuGetVersion,
            DotNetAsterix = semVersion.Substring(version.Length).TrimStart('-'),
            DotNetVersion = dotnetVersion,
            PreReleaseTag = preReleaseTag
        };
    }
}
