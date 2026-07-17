using GitVersion.Extensions;

namespace GitVersion.Git;

internal sealed class ManagedRemoteCollection(ManagedGitRepository repository) : IRemoteCollection
{
    private readonly ManagedGitRepository repository = repository.NotNull();

    public IEnumerator<IRemote> GetEnumerator() => this.repository.Session.Remotes.Cast<IRemote>().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IRemote? this[string name]
    {
        get
        {
            name = name.NotNull();
            return this.repository.Session.Remotes.FirstOrDefault(remote => remote.Name == name);
        }
    }

    public void Remove(string remoteName)
    {
        remoteName = remoteName.NotNull();
        this.repository.CliMutator.RemoveRemote(this.repository.CliWorkingDirectory, remoteName);
        this.repository.Invalidate();
    }

    public void Update(string remoteName, string refSpec)
    {
        remoteName = remoteName.NotNull();
        refSpec = refSpec.NotNull();
        this.repository.CliMutator.AddConfig(this.repository.CliWorkingDirectory, $"remote.{remoteName}.fetch", refSpec);
        this.repository.Invalidate();
    }
}
