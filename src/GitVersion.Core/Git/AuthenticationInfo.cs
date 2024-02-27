namespace GitVersion;

public record AuthenticationInfo
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Token { get; set; }
}
