namespace GitVersion;

internal class Environment : IEnvironment
{
    public string? GetEnvironmentVariable(string variableName) => SysEnv.GetEnvironmentVariable(variableName);

    public void SetEnvironmentVariable(string variableName, string? value) => SysEnv.SetEnvironmentVariable(variableName, value);
}
