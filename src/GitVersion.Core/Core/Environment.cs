namespace GitVersion;

internal class Environment : IEnvironment
{
    public string? GetEnvironmentVariable(string variableName) => System.Environment.GetEnvironmentVariable(variableName);

    public void SetEnvironmentVariable(string variableName, string? value) => System.Environment.SetEnvironmentVariable(variableName, value);
}
