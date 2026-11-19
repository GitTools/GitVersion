namespace GitVersion.Git;

/// <summary>The sole Git implementation since v7.1.</summary>
internal enum GitBackend
{
    Managed
}

/// <summary>Validates the v7 compatibility setting; selector removal is reserved for v8.</summary>
internal static class GitBackendSelector
{
    public const string EnvironmentVariableName = "GITVERSION_GIT_BACKEND";

    public static string ResolveName()
    {
        Resolve();
        return "managed";
    }

    public static GitBackend Resolve()
    {
        var value = SysEnv.GetEnvironmentVariable(EnvironmentVariableName)?.Trim();
        if (string.IsNullOrEmpty(value) || value.Equals("managed", StringComparison.OrdinalIgnoreCase))
        {
            return GitBackend.Managed;
        }

        throw new WarningException(value.Equals("libgit2", StringComparison.OrdinalIgnoreCase)
            ? $"{EnvironmentVariableName}=libgit2 is no longer supported: LibGit2Sharp was removed in v7.1. Unset {EnvironmentVariableName} or set it to 'managed'."
            : $"Unrecognized {EnvironmentVariableName} value '{value}'. The supported value is 'managed'; unset the variable to use the default.");
    }
}
