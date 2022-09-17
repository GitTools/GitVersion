namespace GitVersion.VersionCalculation;

public class NextVersion
{
    public BaseVersion Version { get; set; }

    public SemanticVersion IncrementedVersion { get; set; }

    public NextVersion(SemanticVersion incrementedVersion, BaseVersion baseVersion)
    {
        IncrementedVersion = incrementedVersion;
        Version = baseVersion;
    }

    public override string ToString() => $"{Version} | {IncrementedVersion}";
}
