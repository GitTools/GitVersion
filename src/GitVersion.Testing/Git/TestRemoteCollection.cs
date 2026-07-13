using GitVersion.Extensions;

namespace GitVersion.Testing;

/// <summary>
///     The configured remotes of a <see cref="TestRepository" />.
/// </summary>
public sealed class TestRemoteCollection(TestRepository repository) : IEnumerable<TestRemote>
{
    public TestRemote Add(string name, string url)
    {
        repository.Run("remote", "add", name, url);
        return new(name, url);
    }

    public void RenameRemote(string oldName, string newName)
    {
        if (oldName.IsEquivalentTo(newName))
        {
            return;
        }

        if (this.Any(remote => remote.Name == newName))
        {
            throw new InvalidOperationException($"A remote with the name '{newName}' already exists.");
        }

        repository.Run("remote", "rename", oldName, newName);
    }

    public IEnumerator<TestRemote> GetEnumerator()
    {
        var names = repository
            .Run("remote")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var remotes = names.Select(name => new TestRemote(name, repository.Run("remote", "get-url", name).Trim()));
        return remotes.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
