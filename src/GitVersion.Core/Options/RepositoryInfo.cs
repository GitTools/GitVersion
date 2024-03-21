namespace GitVersion;

public record RepositoryInfo
{
    public string? TargetUrl;
    public string? TargetBranch;
    public string? CommitId;
    public string? ClonePath;
}
