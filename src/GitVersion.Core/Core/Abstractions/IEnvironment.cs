namespace GitVersion;

/// <summary>Provides access to environment variables in the current process.</summary>
public interface IEnvironment
{
    /// <summary>Returns the value of the named environment variable, or <see langword="null"/> if it is not set.</summary>
    string? GetEnvironmentVariable(string variableName);

    /// <summary>Sets the named environment variable to the given value, or removes it when <paramref name="value"/> is <see langword="null"/>.</summary>
    void SetEnvironmentVariable(string variableName, string? value);
}
