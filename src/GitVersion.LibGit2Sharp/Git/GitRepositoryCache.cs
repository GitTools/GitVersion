using System.Collections.Concurrent;
using LibGit2Sharp;

namespace GitVersion.Git;

internal class GitRepositoryCache
{
    private readonly ConcurrentDictionary<string, Branch> cachedBranches = new();
    private readonly ConcurrentDictionary<string, Commit> cachedCommits = new();
    private readonly ConcurrentDictionary<string, Tag> cachedTags = new();
    private readonly ConcurrentDictionary<string, Remote> cachedRemotes = new();
    private readonly ConcurrentDictionary<string, Reference> cachedReferences = new();
    private readonly ConcurrentDictionary<string, RefSpec> cachedRefSpecs = new();

    public Branch GetOrWrap(LibGit2Sharp.Branch innerBranch, Diff repoDiff)
    {
        var cacheKey = innerBranch.Tip is null
            ? $"{innerBranch.RemoteName}/{innerBranch.CanonicalName}"
            : $"{innerBranch.RemoteName}/{innerBranch.CanonicalName}@{innerBranch.Tip.Sha}";
        return cachedBranches.GetOrAdd(cacheKey, _ => new Branch(innerBranch, repoDiff, this));
    }

    public Commit GetOrWrap(LibGit2Sharp.Commit innerCommit, Diff repoDiff)
        => cachedCommits.GetOrAdd(innerCommit.Sha, _ => new Commit(innerCommit, repoDiff, this));

    public Tag GetOrWrap(LibGit2Sharp.Tag innerTag, Diff repoDiff)
        => cachedTags.GetOrAdd(innerTag.CanonicalName, _ => new Tag(innerTag, repoDiff, this));

    public Remote GetOrWrap(LibGit2Sharp.Remote innerRemote)
        => cachedRemotes.GetOrAdd(innerRemote.Name, _ => new Remote(innerRemote, this));

    public Reference GetOrWrap(LibGit2Sharp.Reference innerReference)
        => cachedReferences.GetOrAdd(innerReference.CanonicalName, _ => new Reference(innerReference));

    public RefSpec GetOrWrap(LibGit2Sharp.RefSpec innerRefSpec)
        => cachedRefSpecs.GetOrAdd(innerRefSpec.Specification, _ => new RefSpec(innerRefSpec));
}
