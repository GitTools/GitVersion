namespace Common.Utilities;

public sealed record ReleasePublication(string Name, string Milestone, bool Prerelease, string TargetCommitish)
{
    public bool CloseMilestone => !Prerelease;

    public string[] GitVersionArguments => PrereleaseLabel is { } label
        ? ["--no-normalize", "--commit", TargetCommitish, "--override-config", $"calculation.branches.main.label={label}"]
        : ["--no-normalize", "--commit", TargetCommitish];

    public string? PrereleaseLabel
    {
        get
        {
            if (!Prerelease)
            {
                return null;
            }
            var label = Name[(Name.IndexOf('-') + 1)..];
            var separator = label.LastIndexOf('.');
            return separator >= 0 && label[(separator + 1)..].All(char.IsAsciiDigit) ? label[..separator] : label;
        }
    }

    public static ReleasePublication Create(string version, string targetCommitish)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetCommitish);

        var separator = version.IndexOf('-');
        return new ReleasePublication(version, separator < 0 ? version : version[..separator], separator >= 0, targetCommitish);
    }

    public static string[] DockerVersionTags(string version, string semVersion, bool prerelease)
        => prerelease || version == semVersion ? [semVersion] : [version, semVersion];
}
