namespace GitVersion.Core.Tests.Helpers;

public class TestEnvironment : IEnvironment
{
    private readonly Dictionary<string, string?> map = [];

    public string? GetEnvironmentVariable(string variableName) => this.map.GetValueOrDefault(variableName);

    public void SetEnvironmentVariable(string variableName, string? value) => this.map[variableName] = value;
}
