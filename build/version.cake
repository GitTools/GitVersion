public class BuildVersion
{
    public GitVersion GitVersion { get; private set; }
    public string Version { get; private set; }
    public string Milestone { get; private set; }
    public string SemVersion { get; private set; }
    public string GemVersion { get; private set; }
    public string TfxVersion { get; private set; }

    public static BuildVersion Calculate(ICakeContext context, BuildParameters parameters, GitVersion gitVersion)
    {
        var version = gitVersion.MajorMinorPatch;
        var semVersion = gitVersion.LegacySemVer;
        var tfxVersion = gitVersion.MajorMinorPatch;

        if (!string.IsNullOrWhiteSpace(gitVersion.BuildMetaData)) {
            semVersion += "-" + gitVersion.BuildMetaData;
            tfxVersion += "." + DateTime.UtcNow.ToString("yyMMddHH");
        }

        return new BuildVersion
        {
            GitVersion = gitVersion,
            Milestone  = version,
            Version    = version,
            SemVersion = semVersion,
            GemVersion = semVersion.Replace("-", "."),
            TfxVersion = tfxVersion,
        };
    }
}
