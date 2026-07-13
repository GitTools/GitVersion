namespace GitVersion.Testing;

/// <summary>
///     The tags of a <see cref="TestRepository" />.
/// </summary>
public sealed class TestTagCollection(TestRepository repository) : IEnumerable<TestTag>
{
    /// <summary>
    ///     Creates a lightweight tag pointing at the given commit.
    /// </summary>
    public TestTag Add(string name, TestCommit target) => Add(name, target.Sha);

    /// <summary>
    ///     Creates a lightweight tag pointing at the given committish.
    /// </summary>
    public TestTag Add(string name, string committish)
    {
        repository.Run("tag", name, committish);
        return new(repository, name, repository.RevParse(committish));
    }

    /// <summary>
    ///     Creates an annotated tag pointing at the given commit.
    /// </summary>
    public TestTag Add(string name, TestCommit target, Signature tagger, string message)
    {
        repository.Run(tagger, tagger, ["tag", "--annotate", "--message", message, name, target.Sha]);
        return new(repository, name, target.Sha);
    }

    public void Remove(TestTag tag) => Remove(tag.FriendlyName);

    public void Remove(string friendlyName) => repository.Run("tag", "--delete", friendlyName);

    public IEnumerator<TestTag> GetEnumerator()
    {
        var output = repository.Run("for-each-ref", "refs/tags", "--format=%(refname:short)%00%(objectname)%00%(*objectname)");
        var tags = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line =>
            {
                var fields = line.Split('\0');
                var targetSha = fields[2].Length > 0 ? fields[2] : fields[1];
                return new TestTag(repository, fields[0], targetSha);
            });
        return tags.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
