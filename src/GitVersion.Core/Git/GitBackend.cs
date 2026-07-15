namespace GitVersion.Git;

/// <summary>
/// The Git backend implementations selectable via the <c>GITVERSION_GIT_BACKEND</c>
/// environment variable during the dual-backend window (v7.0–v7.x).
/// </summary>
internal enum GitBackend
{
    /// <summary>The libgit2-based backend (the v7.0 default).</summary>
    LibGit2,

    /// <summary>The managed reader + git CLI mutator backend.</summary>
    Managed
}

/// <summary>
/// The single place where <c>GITVERSION_GIT_BACKEND</c> is interpreted: every composition
/// root selects the backend through this class so production and tests cannot drift, and
/// the v7.1 default flip is a one-line change.
/// </summary>
internal static class GitBackendSelector
{
    public const string EnvironmentVariableName = "GITVERSION_GIT_BACKEND";

    /// <summary>
    /// Resolves the backend from the environment: <c>libgit2</c> (the default when unset)
    /// or <c>managed</c>. Unknown values fail fast — a silently ignored typo would make a
    /// user believe they validated the managed backend while running libgit2.
    /// </summary>
    /// <returns>The selected backend.</returns>
    /// <exception cref="InvalidOperationException">The variable is set to an unrecognized value.</exception>
    public static GitBackend Resolve()
    {
        var value = SysEnv.GetEnvironmentVariable(EnvironmentVariableName)?.Trim();

        return value switch
        {
            null or "" => GitBackend.LibGit2,
            _ when value.Equals("libgit2", StringComparison.OrdinalIgnoreCase) => GitBackend.LibGit2,
            _ when value.Equals("managed", StringComparison.OrdinalIgnoreCase) => GitBackend.Managed,
            _ => throw new InvalidOperationException(
                $"Unrecognized {EnvironmentVariableName} value '{value}'. Valid values are 'libgit2' and 'managed'.")
        };
    }
}
