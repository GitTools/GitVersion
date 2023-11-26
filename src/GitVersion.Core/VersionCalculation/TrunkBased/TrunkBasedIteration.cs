using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.TrunkBased;

[DebuggerDisplay(
    @"\{ Id = {" + nameof(Id) + "}, BranchName = {" + nameof(BranchName) + "}, Depth = {" + nameof(Depth) + "}, NumberOfCommits = {" + nameof(NumberOfCommits) + "}" + @"} \}"
)]
internal record class TrunkBasedIteration
{
    public EffectiveConfiguration Configuration { get; }

    public TrunkBasedIteration? Parent { get; }

    public string Id { get; }

    public ReferenceName BranchName { get; }

    public int Depth { get; }

    public int NumberOfCommits => commits.Count;

    public IReadOnlyCollection<TrunkBasedCommit> Commits => commits;
    private readonly Stack<TrunkBasedCommit> commits = new();

    private readonly Dictionary<ICommit, TrunkBasedCommit> commitLookup = new();

    public TrunkBasedIteration(string id, ReferenceName branchName, EffectiveConfiguration configuration, TrunkBasedIteration? parent)
    {
        Id = id.NotNullOrEmpty();
        Depth = parent?.Depth ?? 0 + 1;
        BranchName = branchName.NotNull();
        Configuration = configuration.NotNull();
        Parent = parent;
    }

    public TrunkBasedCommit CreateCommit(
        ICommit value, ReferenceName branchName, EffectiveConfiguration configuration, VersionField increment)
    {
        TrunkBasedCommit commit;
        if (commits.Any())
            commit = commits.Peek().Append(value, branchName, configuration, increment);
        else
        {
            commit = new TrunkBasedCommit(this, value, branchName, configuration, increment);
        }
        commits.Push(commit);
        commitLookup.Add(value, commit);

        return commit;
    }

    public TrunkBasedCommit? FindCommit(ICommit commit)
    {
        commit.NotNull();

        commitLookup.TryGetValue(commit, out var result);
        return result;
    }
}
