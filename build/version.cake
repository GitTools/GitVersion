public class BuildVersion
{
    public string Version { get; private set; }
    public string SemVersion { get; private set; }
    public string GemVersion { get; private set; }

    public static BuildVersion Calculate(ICakeContext context, BuildParameters parameters, GitVersion gitVersion)
    {
        var version = gitVersion.MajorMinorPatch;
        var semVersion = gitVersion.LegacySemVerPadded;

        if (!string.IsNullOrWhiteSpace(gitVersion.BuildMetaDataPadded)) {
            semVersion += "-" + gitVersion.BuildMetaDataPadded;
        }

        return new BuildVersion
        {
            Version    = version,
            SemVersion = semVersion,
            GemVersion = semVersion.Replace("-", "."),
        };
    }
}
