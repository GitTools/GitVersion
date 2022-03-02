namespace GitVersion;

public interface IRemoteCollection : IEnumerable<IRemote>
{
    IRemote? this[string name] { get; }
    void Remove(string remoteName);
    void Update(string remoteName, string refSpec);
}
