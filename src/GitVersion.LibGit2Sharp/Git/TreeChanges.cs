namespace GitVersion.Git;

internal readonly struct TreeChanges(LibGit2Sharp.TreeChanges innerTreeChanges) : ITreeChanges
{
    private readonly LibGit2Sharp.TreeChanges innerTreeChanges = innerTreeChanges ?? throw new ArgumentNullException(nameof(innerTreeChanges));

    public IReadOnlyList<string> Paths => [.. this.innerTreeChanges.Select(element => element.Path)];
}
