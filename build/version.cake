public class BuildVersion
{
    public string Version { get; private set; }
    public string SemVersion { get; private set; }
    public string NuGetVersion { get; private set; }
    public string DotNetAsterix { get; private set; }
    public string PreReleaseTag { get; private set; }
    public string GemVersion { get; private set; }

    public static BuildVersion Calculate(ICakeContext context, BuildParameters parameters, GitVersion gitVersion)
    {
        var semVersion = gitVersion.LegacySemVerPadded;
        var version = gitVersion.MajorMinorPatch;
        var preReleaseTag = gitVersion.PreReleaseTag;

        var gemVersion = string.IsNullOrEmpty(preReleaseTag)
                        ? version
                        : version + "." + preReleaseTag + "." + gitVersion.BuildMetaDataPadded;

        return new BuildVersion
        {
            Version = version,
            SemVersion = semVersion,
            NuGetVersion = gitVersion.NuGetVersion,
            DotNetAsterix = semVersion.Substring(version.Length).TrimStart('-'),
            PreReleaseTag = preReleaseTag,
            GemVersion = gemVersion
        };
    }
}
