namespace GitVersion;

/// <summary>Identifies the remote repository and the specific commit or branch to version.</summary>
public record RepositoryInfo
{
    /// <summary>Gets or sets the URL of the remote repository to clone or fetch from.</summary>
    public string? TargetUrl;

    /// <summary>Gets or sets the name of the branch to calculate the version for.</summary>
    public string? TargetBranch;

    /// <summary>Gets or sets a specific commit SHA to use instead of the current HEAD.</summary>
    public string? CommitId;

    /// <summary>Gets or sets the local path to which the repository should be cloned when using dynamic repositories.</summary>
    public string? ClonePath;
}
