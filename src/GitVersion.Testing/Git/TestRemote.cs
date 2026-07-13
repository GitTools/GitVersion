namespace GitVersion.Testing;

/// <summary>
///     A lightweight view of a configured remote in a <see cref="TestRepository" />.
/// </summary>
public sealed class TestRemote(string name, string url)
{
    public string Name { get; } = name;

    public string Url { get; } = url;

    public override string ToString() => $"{Name} ({Url})";
}
