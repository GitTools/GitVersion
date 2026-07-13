namespace GitVersion.Testing;

/// <summary>
///     The local and remote-tracking branches of a <see cref="TestRepository" />.
/// </summary>
public sealed class TestBranchCollection(TestRepository repository) : IEnumerable<TestBranch>
{
    /// <summary>
    ///     Looks a branch up by its friendly name, e.g. <c>develop</c> or <c>origin/develop</c>.
    ///     Like the previous git-library indexer this is declared non-nullable but returns <c>null</c>
    ///     when the branch does not exist, so existing <c>== null</c> call sites keep working.
    /// </summary>
    public TestBranch this[string friendlyName]
    {
        get
        {
            var name = friendlyName;
            if (name.StartsWith("refs/heads/", StringComparison.Ordinal))
            {
                name = name["refs/heads/".Length..];
            }
            else if (name.StartsWith("refs/remotes/", StringComparison.Ordinal))
            {
                name = name["refs/remotes/".Length..];
            }
            foreach (var canonicalName in new[] { $"refs/heads/{name}", $"refs/remotes/{name}" })
            {
                if (repository.TryRun(["rev-parse", "--verify", "--quiet", canonicalName]))
                {
                    return new(repository, canonicalName, name);
                }
            }

            return null!;
        }
    }

    public TestBranch Add(string name, TestCommit commit) => Add(name, commit.Sha);

    public TestBranch Add(string name, string committish)
    {
        repository.Run("branch", name, committish);
        return new(repository, $"refs/heads/{name}", name);
    }

    /// <summary>
    ///     Deletes the branch; a no-op when the branch does not exist, mirroring the previous git-library behavior.
    /// </summary>
    public void Remove(string friendlyName)
    {
        var branch = this[friendlyName];
        if (branch is not null)
        {
            Remove(branch);
        }
    }

    public void Remove(TestBranch branch)
    {
        if (branch.IsRemote)
        {
            repository.Run("branch", "--remotes", "--delete", "--force", branch.FriendlyName);
        }
        else
        {
            repository.Run("branch", "--delete", "--force", branch.FriendlyName);
        }
    }

    public IEnumerator<TestBranch> GetEnumerator()
    {
        var output = repository.Run("for-each-ref", "refs/heads", "refs/remotes", "--format=%(refname)");
        var branches = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(canonicalName =>
            {
                var friendlyName = canonicalName.StartsWith("refs/heads/", StringComparison.Ordinal)
                    ? canonicalName["refs/heads/".Length..]
                    : canonicalName["refs/remotes/".Length..];
                return new TestBranch(repository, canonicalName, friendlyName);
            });
        return branches.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
