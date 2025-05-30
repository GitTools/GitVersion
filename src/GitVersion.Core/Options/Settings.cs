namespace GitVersion;

public record Settings
{
    public bool NoFetch;
    public bool NoCache;
    public bool NoNormalize;
    public bool OnlyTrackedBranches = false;
    public bool AllowShallow = false;
}
