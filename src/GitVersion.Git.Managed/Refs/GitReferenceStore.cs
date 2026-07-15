namespace GitVersion.Git;

/// <summary>
/// Provides read-only access to the references of a Git repository: loose refs under
/// <c>refs/</c>, the <c>packed-refs</c> file (including peeled targets), and per-worktree
/// refs such as <c>HEAD</c>.
/// </summary>
/// <remarks>
/// Loose references take precedence over entries in <c>packed-refs</c>, matching git.
/// </remarks>
internal sealed class GitReferenceStore
{
    private const string RefsPrefix = "refs/";

    // Matches git's limit on the depth of nested symbolic references (MAXDEPTH in refs.c).
    private const int MaxSymbolicReferenceDepth = 5;

    private readonly string gitDirectory;
    private readonly string commonDirectory;
    private readonly Lazy<Dictionary<string, GitReference>> packedReferences;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitReferenceStore"/> class.
    /// </summary>
    /// <param name="gitDirectory">The (per-worktree) git directory, which contains <c>HEAD</c>.</param>
    /// <param name="commonDirectory">
    /// The common git directory, which contains <c>refs/</c> and <c>packed-refs</c>.
    /// Defaults to <paramref name="gitDirectory"/>.
    /// </param>
    public GitReferenceStore(string gitDirectory, string? commonDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(gitDirectory);

        this.gitDirectory = Path.GetFullPath(gitDirectory);
        this.commonDirectory = commonDirectory is null ? this.gitDirectory : Path.GetFullPath(commonDirectory);
        this.packedReferences = new(ReadPackedReferences);
    }

    /// <summary>
    /// Reads a single reference by its canonical name, without following symbolic references.
    /// </summary>
    /// <param name="canonicalName">The canonical name, e.g. <c>refs/heads/main</c> or <c>HEAD</c>.</param>
    /// <returns>The reference, or <see langword="null"/> if it does not exist.</returns>
    public GitReference? GetReference(string canonicalName)
    {
        ArgumentNullException.ThrowIfNull(canonicalName);

        return ReadLooseReference(canonicalName)
            ?? (this.packedReferences.Value.TryGetValue(canonicalName, out var packed) ? packed : null);
    }

    /// <summary>
    /// Reads the <c>HEAD</c> reference of the (worktree's) repository.
    /// </summary>
    /// <returns>The <c>HEAD</c> reference, or <see langword="null"/> if it does not exist.</returns>
    public GitReference? GetHead() => GetReference("HEAD");

    /// <summary>
    /// Resolves a reference to a direct reference, following symbolic references.
    /// </summary>
    /// <param name="canonicalName">The canonical name, e.g. <c>HEAD</c> or <c>refs/heads/main</c>.</param>
    /// <returns>
    /// The direct reference at the end of the symbolic chain, or <see langword="null"/> when the
    /// reference does not exist (e.g. <c>HEAD</c> pointing at an unborn branch).
    /// </returns>
    public GitReference? Resolve(string canonicalName)
    {
        var current = GetReference(canonicalName);
        var depth = 0;

        while (current is { IsSymbolic: true })
        {
            if (++depth > MaxSymbolicReferenceDepth)
            {
                throw new GitObjectStoreException($"The symbolic reference '{canonicalName}' is nested more than {MaxSymbolicReferenceDepth} levels deep.");
            }

            current = GetReference(current.SymbolicTargetName!);
        }

        return current;
    }

    /// <summary>
    /// Resolves a reference to the object id it (ultimately) points at.
    /// </summary>
    /// <param name="canonicalName">The canonical name, e.g. <c>HEAD</c> or <c>refs/tags/v1.0.0</c>.</param>
    /// <returns>The object id, or <see langword="null"/> when the reference does not exist.</returns>
    public GitObjectId? ResolveToObjectId(string canonicalName) => Resolve(canonicalName)?.ObjectId;

    /// <summary>
    /// Enumerates the references whose canonical name starts with the given prefix,
    /// in ordinal name order. Loose references shadow packed references of the same name.
    /// </summary>
    /// <param name="prefix">The prefix to filter by, e.g. <c>refs/</c>, <c>refs/heads/</c> or <c>refs/tags/</c>.</param>
    /// <returns>The matching references.</returns>
    public IEnumerable<GitReference> EnumerateReferences(string prefix = RefsPrefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);

        var references = new SortedDictionary<string, GitReference>(StringComparer.Ordinal);

        foreach (var packed in this.packedReferences.Value.Values
                     .Where(reference => reference.CanonicalName.StartsWith(prefix, StringComparison.Ordinal)))
        {
            references[packed.CanonicalName] = packed;
        }

        var refsRoot = Path.Combine(this.commonDirectory, "refs");

        if (Directory.Exists(refsRoot))
        {
            foreach (var file in Directory.EnumerateFiles(refsRoot, "*", SearchOption.AllDirectories))
            {
                // Skip transient lock files created while git updates a reference, matching git/libgit2.
                if (file.EndsWith(".lock", StringComparison.Ordinal))
                {
                    continue;
                }

                var canonicalName = RefsPrefix + Path.GetRelativePath(refsRoot, file).Replace(Path.DirectorySeparatorChar, '/');

                if (!canonicalName.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                if (ReadLooseReference(canonicalName) is { } reference)
                {
                    references[canonicalName] = reference;
                }
            }
        }

        return references.Values;
    }

    private GitReference? ReadLooseReference(string canonicalName)
    {
        // HEAD and other pseudo-refs (ORIG_HEAD, FETCH_HEAD, ...) are per-worktree and live in
        // the git directory; everything under refs/ is shared and lives in the common directory.
        var directory = canonicalName.StartsWith(RefsPrefix, StringComparison.Ordinal)
            ? this.commonDirectory
            : this.gitDirectory;

        var path = Path.Combine(directory, canonicalName.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(path))
        {
            return null;
        }

        string? line;

        using (var reader = new StreamReader(path, Encoding.UTF8))
        {
            line = reader.ReadLine();
        }

        line = line?.Trim();

        if (string.IsNullOrEmpty(line))
        {
            return null;
        }

        if (line.StartsWith("ref: ", StringComparison.Ordinal))
        {
            return new()
            {
                CanonicalName = canonicalName,
                SymbolicTargetName = line["ref: ".Length..].Trim()
            };
        }

        return new()
        {
            CanonicalName = canonicalName,
            ObjectId = GitObjectId.Parse(line)
        };
    }

    private Dictionary<string, GitReference> ReadPackedReferences()
    {
        var references = new Dictionary<string, GitReference>(StringComparer.Ordinal);
        var packedRefsPath = Path.Combine(this.commonDirectory, "packed-refs");

        if (!File.Exists(packedRefsPath))
        {
            return references;
        }

        string? previousName = null;

        foreach (var rawLine in File.ReadLines(packedRefsPath, Encoding.UTF8))
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith('^'))
            {
                // A peeled line holds the fully-peeled target of the preceding annotated tag.
                if (previousName is null)
                {
                    throw new GitObjectStoreException("The packed-refs file is malformed: a peeled line has no preceding reference.");
                }

                var peeled = references[previousName];
                references[previousName] = new()
                {
                    CanonicalName = peeled.CanonicalName,
                    ObjectId = peeled.ObjectId,
                    PeeledObjectId = GitObjectId.Parse(line[1..]),
                    IsPacked = true
                };
                continue;
            }

            var separator = line.IndexOf(' ');

            if (separator <= 0 || separator == line.Length - 1)
            {
                throw new GitObjectStoreException("The packed-refs file is malformed: a line does not contain an object id and a reference name.");
            }

            var name = line[(separator + 1)..];
            references[name] = new()
            {
                CanonicalName = name,
                ObjectId = GitObjectId.Parse(line[..separator]),
                IsPacked = true
            };
            previousName = name;
        }

        return references;
    }
}
