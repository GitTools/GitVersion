namespace GitVersion.Git;

internal sealed class TreeChanges(LibGit2Sharp.TreeChanges innerTreeChanges) : ITreeChanges
{
    private readonly LibGit2Sharp.TreeChanges innerTreeChanges = innerTreeChanges ?? throw new ArgumentNullException(nameof(innerTreeChanges));

    public IEnumerable<string> Paths => this.innerTreeChanges.Select(element => element.Path);
}
