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
internal sealed class MainlineIteration
{
    public IBranchConfiguration Configuration { get; }

    public MainlineCommit? BranchedFrom
    {
        get => this.branchedFrom?.Value;
        internal set => this.branchedFrom = new(() => value);
    }

    private Lazy<MainlineCommit?>? branchedFrom;

    internal void SetBranchPointFactory(Func<MainlineCommit?> factory) => this.branchedFrom = new(factory);

    internal Func<bool>? CapturePreviousCommit { get; set; }

    private EffectiveConfiguration? effectiveConfiguration;

    public EffectiveConfiguration GetEffectiveConfiguration(IGitVersionConfiguration configuration)
    {
        if (this.effectiveConfiguration is not null)
        {
            return this.effectiveConfiguration;
        }

        var branchConfiguration = Configuration;

        if (branchConfiguration.Increment != IncrementStrategy.Inherit || (Commits.FirstOrDefault() ?? BranchedFrom) is not { } commit)
        {
            return this.effectiveConfiguration = new EffectiveConfiguration(configuration, branchConfiguration);
        }

        var parentConfiguration = commit.GetEffectiveConfiguration(configuration);
        branchConfiguration = branchConfiguration.Inherit(parentConfiguration);

        return this.effectiveConfiguration = new EffectiveConfiguration(configuration, branchConfiguration);
    }

    public MainlineIteration? ParentIteration { get; }

    public MainlineCommit? ParentCommit { get; }

    public string Id { get; }

    public ReferenceName BranchName { get; }

    public int Depth { get; }

    public int NumberOfCommits => this.commits.Count;

    public IReadOnlyCollection<MainlineCommit> Commits => this.commits;
    private readonly Stack<MainlineCommit> commits = new();

    private readonly Dictionary<ICommit, MainlineCommit> commitLookup = [];

    public MainlineIteration(string id, ReferenceName branchName, IBranchConfiguration configuration,
        MainlineIteration? parentIteration, MainlineCommit? parentCommit)
    {
        Id = id.NotNullOrEmpty();
        Depth = (parentIteration?.Depth ?? 0) + 1;
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
        this.commits.Push(commit);

        if (value is not null)
        {
            this.commitLookup.Add(value, commit);
        }

        return commit;
    }

    public MainlineCommit? FindCommit(ICommit commit)
    {
        commit.NotNull();

        this.commitLookup.TryGetValue(commit, out var result);
        return result;
    }

    internal IEnumerable<MainlineCommit> GetHistory(MainlineCommit? tip = null)
        => GetCommitsInReverseOrder(includeAncestry: true, tip).Reverse();

    internal IEnumerable<MainlineCommit> GetCommitsInReverseOrder(bool includeAncestry, MainlineCommit? tip = null)
    {
        var commit = tip ?? Commits.LastOrDefault();
        while (commit is not null)
        {
            yield return commit;
            if (commit.Predecessor is null)
            {
                CapturePreviousCommit?.Invoke();
            }
            commit = commit.Predecessor;
        }
        if (includeAncestry && BranchedFrom is { } branchPoint)
        {
            foreach (var ancestor in branchPoint.Iteration.GetCommitsInReverseOrder(true, branchPoint))
            {
                yield return ancestor;
            }
        }
    }
}
