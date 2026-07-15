using System.Collections.Concurrent;
using GitVersion.Extensions;

namespace GitVersion.Git;

/// <summary>
/// An immutable snapshot of the repository state (objects, references, configuration) with
/// per-snapshot wrapper caches. Mutating operations replace the current session on the owning
/// <see cref="ManagedGitRepository"/> so that changes made through the git CLI become visible.
/// </summary>
internal sealed class ManagedRepositorySession : IDisposable
{
    private const string RemoteSectionName = "remote";

    private readonly ManagedGitRepository repository;
    private readonly ConcurrentDictionary<GitObjectId, ManagedCommit> commits = new();
    private readonly ConcurrentDictionary<GitObjectId, IReadOnlyList<string>> diffPathsCache = new();
    private readonly Lazy<GitConfigurationFile> configuration;
    private readonly Lazy<IReadOnlyList<ManagedBranch>> branches;
    private readonly Lazy<Dictionary<string, ManagedBranch>> branchesByCanonicalName;
    private readonly Lazy<IReadOnlyList<ManagedTag>> tags;
    private readonly Lazy<IReadOnlyList<ManagedReference>> references;
    private readonly Lazy<IReadOnlyList<ManagedRemote>> remotes;

    public ManagedRepositorySession(ManagedGitRepository repository, GitRepositoryLayout layout)
    {
        this.repository = repository.NotNull();
        Layout = layout.NotNull();
        ObjectStore = layout.CreateObjectStore();
        ReferenceStore = layout.CreateReferenceStore();
        Walker = new(ObjectStore);
        TreeDiff = new(ObjectStore);
        StatusCalculator = new(layout, ObjectStore);
        this.configuration = new(() => GitConfigurationFile.Load(Path.Combine(Layout.CommonDirectory, "config")));
        this.branches = new(ReadBranches);
        this.branchesByCanonicalName = new(() => this.branches.Value.ToDictionary(branch => branch.Name.Canonical, StringComparer.Ordinal));
        this.tags = new(() => [.. EnumerateStoreReferencesInLibGit2Order("refs/tags/").Select(reference => new ManagedTag(reference, this.repository))]);
        this.references = new(() => [.. EnumerateStoreReferencesInLibGit2Order("refs/").Select(reference => new ManagedReference(reference))]);
        this.remotes = new(ReadRemotes);
    }

    public GitRepositoryLayout Layout { get; }
    public GitObjectStore ObjectStore { get; }
    public GitReferenceStore ReferenceStore { get; }
    public GitRevisionWalker Walker { get; }
    public GitTreeDiff TreeDiff { get; }
    public GitStatusCalculator StatusCalculator { get; }
    public GitConfigurationFile Configuration => this.configuration.Value;

    public IReadOnlyList<ManagedBranch> Branches => this.branches.Value;
    public IReadOnlyList<ManagedTag> Tags => this.tags.Value;
    public IReadOnlyList<ManagedReference> References => this.references.Value;
    public IReadOnlyList<ManagedRemote> Remotes => this.remotes.Value;

    public GitObjectId? HeadTipId => ReferenceStore.Resolve("HEAD")?.ObjectId;

    public ManagedBranch GetHead()
    {
        var head = ReferenceStore.GetHead()
            ?? throw new InvalidOperationException("The repository has no HEAD reference.");

        if (head.IsSymbolic)
        {
            var targetName = head.SymbolicTargetName!;
            var tipId = ReferenceStore.Resolve(targetName)?.ObjectId;
            var tip = tipId is { } id ? TryGetCommit(id) : null;
            return this.branchesByCanonicalName.Value.GetValueOrDefault(targetName)
                ?? new ManagedBranch(new(targetName), tip, this.repository);
        }

        // Detached HEAD: libgit2 exposes it as a branch named "(no branch)".
        var commit = head.ObjectId is { } commitId ? TryGetCommit(commitId) : null;
        return new(new("(no branch)"), commit, this.repository);
    }

    public ManagedBranch? FindBranch(string name)
    {
        var branches = this.branchesByCanonicalName.Value;

        if (name.StartsWith("refs/", StringComparison.Ordinal))
        {
            return branches.GetValueOrDefault(name);
        }

        return branches.GetValueOrDefault(ReferenceName.LocalBranchPrefix + name)
            ?? branches.GetValueOrDefault(ReferenceName.RemoteTrackingBranchPrefix + name);
    }

    public ManagedReference? GetReference(string canonicalName) =>
        ReferenceStore.GetReference(canonicalName) is { } reference ? new ManagedReference(reference) : null;

    public IEnumerable<IReference> EnumerateReferences(string prefix) =>
        EnumerateStoreReferencesInLibGit2Order(prefix).Select(reference => new ManagedReference(reference));

    /// <summary>
    /// Enumerates references the way libgit2's refdb iterates them: loose references first
    /// (in name order), then the packed references that are not shadowed by a loose one
    /// (in name order). The store itself yields git's globally sorted for-each-ref order.
    /// </summary>
    private IEnumerable<GitReference> EnumerateStoreReferencesInLibGit2Order(string prefix)
    {
        var storeReferences = ReferenceStore.EnumerateReferences(prefix).ToList();
        return storeReferences.Where(reference => !reference.IsPacked)
            .Concat(storeReferences.Where(reference => reference.IsPacked));
    }

    public ManagedCommit GetCommit(GitObjectId objectId) =>
        TryGetCommit(objectId)
            ?? throw new InvalidOperationException($"The commit '{objectId}' does not exist in the repository.");

    public ManagedCommit? TryGetCommit(GitObjectId objectId)
    {
        if (this.commits.TryGetValue(objectId, out var existing))
        {
            return existing;
        }

        // The walker's parsed-commit cache is the single parse per session; walks
        // load most commits first, so wrapping reuses those parses.
        return Walker.TryLoadCommit(objectId) is { } innerCommit
            ? WrapCommit(innerCommit)
            : null;
    }

    public ManagedCommit WrapCommit(GitCommit innerCommit) =>
        this.commits.GetOrAdd(innerCommit.Sha, _ => new(innerCommit, this.repository));

    public IReadOnlyList<string> GetDiffPaths(ManagedCommit commit) =>
        this.diffPathsCache.GetOrAdd(commit.ObjectId, _ =>
        {
            GitObjectId? parentTreeId = commit.FirstParentId is { } parentId ? TryGetCommit(parentId)?.TreeId : null;
            return TreeDiff.GetChangedPaths(parentTreeId, commit.TreeId);
        });

    /// <summary>
    /// Returns what libgit2 exposes as a tag's target sha: for an annotated tag the sha of
    /// the object the tag annotation points at (a single dereference), for a lightweight
    /// tag the ref target itself.
    /// </summary>
    public string GetTagTargetSha(GitReference reference)
    {
        var targetId = reference.ObjectId
            ?? throw new InvalidOperationException($"The tag '{reference.CanonicalName}' does not point at an object.");

        if (!ObjectStore.TryGetObject(targetId, out var stream, out var objectType))
        {
            throw new InvalidOperationException($"The object '{targetId}' does not exist in the repository.");
        }

        using (stream)
        {
            return objectType == GitObjectTypes.Tag
                ? GitTagReader.Read(stream, targetId).Target.ToString()
                : targetId.ToString();
        }
    }

    public ICommit PeelToCommit(GitReference reference)
    {
        if (reference.PeeledObjectId is { } peeledId)
        {
            return GetCommit(peeledId);
        }

        var targetId = reference.ObjectId
            ?? throw new InvalidOperationException($"The reference '{reference.CanonicalName}' does not point at an object.");

        while (true)
        {
            if (!ObjectStore.TryGetObject(targetId, out var stream, out var objectType))
            {
                throw new InvalidOperationException($"The object '{targetId}' does not exist in the repository.");
            }

            switch (objectType)
            {
                case GitObjectTypes.Commit:
                    stream.Dispose();
                    return GetCommit(targetId);
                case GitObjectTypes.Tag:
                    using (stream)
                    {
                        targetId = GitTagReader.Read(stream, targetId).Target;
                    }

                    break;
                default:
                    stream.Dispose();
                    throw new InvalidOperationException($"The reference '{reference.CanonicalName}' does not ultimately point at a commit.");
            }
        }
    }

    public bool IsTracking(ManagedBranch branch)
    {
        if (branch.IsRemote || branch.IsDetachedHead)
        {
            return false;
        }

        var friendlyName = branch.Name.Friendly;
        var remoteName = Configuration.GetString("branch", friendlyName, "remote");
        var mergeReference = Configuration.GetString("branch", friendlyName, "merge");

        if (remoteName is null || mergeReference is null)
        {
            return false;
        }

        if (remoteName == ".")
        {
            return ReferenceStore.GetReference(mergeReference) is not null;
        }

        if (!mergeReference.StartsWith(ReferenceName.LocalBranchPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var upstream = $"{ReferenceName.RemoteTrackingBranchPrefix}{remoteName}/{mergeReference[ReferenceName.LocalBranchPrefix.Length..]}";
        return ReferenceStore.GetReference(upstream) is not null;
    }

    public void Dispose() => ObjectStore.Dispose();

    private IReadOnlyList<ManagedBranch> ReadBranches()
    {
        // libgit2's branch iterator walks the whole refdb once (loose first, then packed)
        // and filters to branch namespaces, so local and remote-tracking branches interleave
        // in that reference order.
        var result = new List<ManagedBranch>();

        foreach (var reference in EnumerateStoreReferencesInLibGit2Order("refs/"))
        {
            if (!reference.CanonicalName.StartsWith(ReferenceName.LocalBranchPrefix, StringComparison.Ordinal)
                && !reference.CanonicalName.StartsWith(ReferenceName.RemoteTrackingBranchPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var objectId = reference.ObjectId
                ?? (reference.IsSymbolic ? ReferenceStore.Resolve(reference.CanonicalName)?.ObjectId : null);
            var tip = objectId is { } id ? TryGetCommit(id) : null;
            result.Add(new(new(reference.CanonicalName), tip, this.repository));
        }

        return result;
    }

    private IReadOnlyList<ManagedRemote> ReadRemotes() =>
        [.. Configuration.GetSubsections(RemoteSectionName)
            .Select(name => new ManagedRemote(
                name,
                Configuration.GetString(RemoteSectionName, name, "url") ?? string.Empty,
                Configuration.GetAll(RemoteSectionName, name, "fetch"),
                Configuration.GetAll(RemoteSectionName, name, "push")))];
}
