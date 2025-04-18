using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation.Mainline;

[DebuggerDisplay(
    @"\{ Id = {" + nameof(Id) + "}, " +
    "BranchName = {" + nameof(BranchName) + "}, " +
    "Depth = {" + nameof(Depth) + "}, " +
    "NumberOfCommits = {" + nameof(NumberOfCommits) + "}" + @"} \}"
)]
internal record MainlineIteration
{
    public IBranchConfiguration Configuration { get; }

    private EffectiveConfiguration? effectiveConfiguration;

    public EffectiveConfiguration GetEffectiveConfiguration(IGitVersionConfiguration configuration)
    {
        if (this.effectiveConfiguration is not null)
        {
            return this.effectiveConfiguration;
        }

        var branchConfiguration = Configuration;

        if (branchConfiguration.Increment == IncrementStrategy.Inherit && Commits.FirstOrDefault() is { } commit)
        {
            var parentConfiguration = commit.GetEffectiveConfiguration(configuration);
            branchConfiguration = branchConfiguration.Inherit(parentConfiguration);
        }

        return this.effectiveConfiguration = new EffectiveConfiguration(configuration, branchConfiguration);
    }

    public MainlineIteration? ParentIteration { get; }

    public MainlineCommit? ParentCommit { get; }

    public string Id { get; }

    public ReferenceName BranchName { get; }

    public int Depth { get; }

    public int NumberOfCommits => commits.Count;

    public IReadOnlyCollection<MainlineCommit> Commits => commits;
    private readonly Stack<MainlineCommit> commits = new();

    private readonly Dictionary<ICommit, MainlineCommit> commitLookup = [];

    public MainlineIteration(string id, ReferenceName branchName, IBranchConfiguration configuration,
        MainlineIteration? parentIteration, MainlineCommit? parentCommit)
    {
        Id = id.NotNullOrEmpty();
        Depth = parentIteration?.Depth ?? 0 + 1;
        BranchName = branchName.NotNull();
        Configuration = configuration.NotNull();
        ParentIteration = parentIteration;
        ParentCommit = parentCommit;
    }

    public MainlineCommit CreateCommit(
        ICommit? value, ReferenceName branchName, IBranchConfiguration configuration)
    {
        var commit = this.commits.Count != 0
            ? this.commits.Peek().Append(value, branchName, configuration)
            : new MainlineCommit(this, value, branchName, configuration);
        commits.Push(commit);

        if (value is not null)
        {
            commitLookup.Add(value, commit);
        }

        return commit;
    }

    public MainlineCommit? FindCommit(ICommit commit)
    {
        commit.NotNull();

        commitLookup.TryGetValue(commit, out var result);
        return result;
    }
}
