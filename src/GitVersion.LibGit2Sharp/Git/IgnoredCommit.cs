using GitVersion.Extensions;

namespace GitVersion;

internal sealed class IgnoredCommit : ICommit
{
    private readonly IgnoredState ignoredState;

    public IgnoredCommit(ICommit commit, IgnoredState ignoredState)
    {
        this.commit = commit.NotNull();
        this.ignoredState = ignoredState.NotNull();
    }

    public IgnoredState IgnoredState => ignoredState;

    public IEnumerable<ICommit> Parents => commit.Parents;

    public DateTimeOffset When => commit.When;

    public string Message => commit.Message;

    public IObjectId Id => commit.Id;

    public string Sha => commit.Sha;

    private readonly ICommit commit;

    public int CompareTo(ICommit other) => commit.CompareTo(other);

    public int CompareTo(IGitObject other) => commit.CompareTo(other);

    public bool Equals(ICommit? other) => commit.Equals(other);

    public bool Equals(IGitObject? other) => commit.Equals(other);
}
