namespace GitVersion.Git;

/// <summary>Holds credentials used when authenticating with a remote Git repository.</summary>
public record AuthenticationInfo
{
    /// <summary>Gets or sets the username for basic authentication.</summary>
    public string? Username { get; set; }

    /// <summary>Gets or sets the password for basic authentication.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets the personal access token used instead of a username/password pair.</summary>
    public string? Token { get; set; }
}
