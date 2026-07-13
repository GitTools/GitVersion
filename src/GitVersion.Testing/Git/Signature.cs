namespace GitVersion.Testing;

/// <summary>
///     An author/committer/tagger identity with a fixed timestamp, mirroring the shape of a git signature.
/// </summary>
public sealed record Signature(string Name, string Email, DateTimeOffset When);
