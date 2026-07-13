namespace GitVersion.Testing;

/// <summary>
///     Direct reference manipulation for a <see cref="TestRepository" />.
/// </summary>
public sealed class TestReferenceCollection(TestRepository repository)
{
    public void Add(string canonicalName, string targetSha) => repository.Run("update-ref", canonicalName, targetSha);

    public void Add(string canonicalName, TestCommit target) => Add(canonicalName, target.Sha);
}
