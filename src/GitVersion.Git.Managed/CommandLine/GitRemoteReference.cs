namespace GitVersion.Git;

/// <summary>A remote reference as advertised by the remote (e.g. by <c>git ls-remote</c>).</summary>
internal sealed record GitRemoteReference(string CanonicalName, string TargetSha);
