using GitVersion.Git;

namespace GitVersion.VersionCalculation;

// An external source is represented by a source object with a null commit, not by a missing source object.
internal sealed record SemanticVersionSource(string Description, SemanticVersion Version, ICommit? Commit, VersionField Increment);

// Keep the public IBaseVersion contract as the legacy calculation/counting view.
// The selected semantic source survives independently of the counting anchor.
internal sealed record ResolvedBaseVersion(IBaseVersion Candidate, SemanticVersionSource SemVerSource, ICommit? CommitCountSource) : IBaseVersion
{
    public string Source => Candidate.Source;
    public SemanticVersion SemanticVersion => Candidate.SemanticVersion;
    public VersionField Increment => VersionField.None;
    public ICommit? BaseVersionSource => CommitCountSource;
}
