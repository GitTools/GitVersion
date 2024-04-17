using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation.TrunkBased;

[DebuggerDisplay(
    @"\{ Id = {" + nameof(Id) + "}, " +
    "BranchName = {" + nameof(BranchName) + "}, " +
    "Depth = {" + nameof(Depth) + "}, " +
    "NumberOfCommits = {" + nameof(NumberOfCommits) + "}" + @"} \}"
)]
internal record TrunkBasedIteration
{
    public IBranchConfiguration Configuration { get; }

    private EffectiveConfiguration? effectiveConfiguration;

    public EffectiveConfiguration GetEffectiveConfiguration(IGitVersionConfiguration configuration)
    {
        if (effectiveConfiguration is not null) return effectiveConfiguration;

        IBranchConfiguration branchConfiguration = Configuration;

        if (branchConfiguration.Increment == IncrementStrategy.Inherit && Commits.FirstOrDefault() is TrunkBasedCommit commit)
        {
            var parentConfiguration = commit.GetEffectiveConfiguration(configuration);
            branchConfiguration = branchConfiguration.Inherit(parentConfiguration);
        }

        return effectiveConfiguration = new EffectiveConfiguration(configuration, branchConfiguration);
    }

    public TrunkBasedIteration? ParentIteration { get; }

    public TrunkBasedCommit? ParentCommit { get; }

    public string Id { get; }

    public ReferenceName BranchName { get; }

    public int Depth { get; }

    public int NumberOfCommits => commits.Count;

    public IReadOnlyCollection<TrunkBasedCommit> Commits => commits;
    private readonly Stack<TrunkBasedCommit> commits = new();

    private readonly Dictionary<ICommit, TrunkBasedCommit> commitLookup = [];

    public TrunkBasedIteration(string id, ReferenceName branchName, IBranchConfiguration configuration,
        TrunkBasedIteration? parentIteration, TrunkBasedCommit? parentCommit)
    {
        Id = id.NotNullOrEmpty();
        Depth = parentIteration?.Depth ?? 0 + 1;
        BranchName = branchName.NotNull();
        Configuration = configuration.NotNull();
        ParentIteration = parentIteration;
        ParentCommit = parentCommit;
    }

    public TrunkBasedCommit CreateCommit(
        ICommit value, ReferenceName branchName, IBranchConfiguration configuration)
    {
        TrunkBasedCommit commit;
        if (commits.Count != 0)
            commit = commits.Peek().Append(value, branchName, configuration); //, increment);
        else
        {
            commit = new TrunkBasedCommit(this, value, branchName, configuration); //, increment);
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
