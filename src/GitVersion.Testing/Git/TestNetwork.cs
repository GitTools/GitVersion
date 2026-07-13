namespace GitVersion.Testing;

/// <summary>
///     Network-related state of a <see cref="TestRepository" />.
/// </summary>
public sealed class TestNetwork(TestRepository repository)
{
    public TestRemoteCollection Remotes { get; } = new(repository);
}
