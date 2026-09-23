using GitVersion.Configuration;
using GitVersion.Git;

namespace GitVersion;

/// <summary>
/// Produces commit logs from a single cached revision walk instead of one walk per query.
/// </summary>
/// <remarks>
/// This is deliberately kept out of the public <see cref="IRepositoryStore"/>: it trades guarantees which that
/// interface makes (a stable order for equally timed commits, no caching assumptions about the arguments) for
/// speed, and adding a member to a public interface would break every external implementation of it. Callers
/// therefore have to treat it as optional and fall back to <see cref="IRepositoryStore.GetCommitLog"/> when the
/// store they were given does not implement it.
/// </remarks>
internal interface ICachedCommitLogProvider
{
    /// <summary>
    /// Returns the commits reachable between <paramref name="baseVersionSource"/> and <paramref name="currentCommit"/>,
    /// respecting ignore rules, with the commits identified by <paramref name="excludedShas"/> and all of their
    /// ancestors left out as well. The ancestor walk of <paramref name="excludedShas"/> stops at ignored commits,
    /// whereas the one of <paramref name="baseVersionSource"/> passes through them.
    /// </summary>
    /// <remarks>
    /// The result is derived from a single cached revision walk rather than from one walk per call. It holds the
    /// same commits as <see cref="IRepositoryStore.GetCommitLog"/> returns, but commits which share a committer
    /// timestamp may come out in a different order, because a revision walk breaks such ties through the shape of
    /// its priority queue, which changes when commits are hidden.
    /// <para>
    /// Results are cached per <paramref name="excludedShas"/> instance. The set therefore must not be modified
    /// after it has been passed in, and it must be one whose equality is by reference — which every caller in
    /// this assembly satisfies by passing a <see cref="HashSet{T}"/>.
    /// </para>
    /// </remarks>
    IReadOnlyList<ICommit> GetCommitLog(
        ICommit? baseVersionSource, ICommit currentCommit, IIgnoreConfiguration ignore, IReadOnlySet<string> excludedShas);
}
