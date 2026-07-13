namespace GitVersion.Testing;

/// <summary>
///     Repository-local configuration of a <see cref="TestRepository" />.
/// </summary>
public sealed class TestConfig(TestRepository repository)
{
    public void Set(string key, string value) => repository.Run("config", key, value);

    public string? Get(string key)
    {
        var found = repository.TryRun(["config", "--get", key], out var output);
        return found ? output.Trim() : null;
    }
}
