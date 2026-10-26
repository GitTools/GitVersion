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

    public ICommit? BranchedFrom { get; }

    public MainlineCommit? GetBranchedFrom()
    {
        if (BranchedFrom is null)
        {
            return null;
        }
        MainlineCommit? result = null;
        for (MainlineIteration? i = this; i != null && result == null; i = i.ParentIteration)
        {
            result = i.FindCommit(BranchedFrom!);
        }
        return result;
    }

    public ICommit? BranchedTo { get; }

    public MainlineCommit? GetBranchedTo()
    {
        if (ParentIteration is not null)
        {
            return ParentIteration.FindCommit(BranchedTo!);
        }
        return null;
    }

    private EffectiveConfiguration? effectiveConfiguration;

    public EffectiveConfiguration GetEffectiveConfiguration(IGitVersionConfiguration configuration)
    {
        if (this.effectiveConfiguration is not null)
        {
            return this.effectiveConfiguration;
        }
        var branchConfiguration = Configuration;

        if (branchConfiguration.Increment != IncrementStrategy.Inherit || (Commits.FirstOrDefault()
            ?? ParentIteration?.FindCommit(BranchedFrom)) is not { } commit)
        {
            return this.effectiveConfiguration = new EffectiveConfiguration(configuration, branchConfiguration);
        }

        var parentConfiguration = commit.GetEffectiveConfiguration(configuration);
        branchConfiguration = branchConfiguration.Inherit(parentConfiguration);

        return this.effectiveConfiguration = new EffectiveConfiguration(configuration, branchConfiguration);
    }

    public MainlineIteration? ParentIteration { get; }

    public string Id { get; }

    public ReferenceName BranchName { get; }

    public int Depth { get; }

    public int NumberOfCommits => this.commits.Count;

    public IReadOnlyCollection<MainlineCommit> Commits => this.commits;
    private readonly Stack<MainlineCommit> commits = new();

    private readonly Dictionary<ICommit, MainlineCommit> commitLookup = [];

    public MainlineIteration(string id, ReferenceName branchName, IBranchConfiguration configuration,
        MainlineIteration? parentIteration, ICommit? branchedFrom, ICommit? branchedTo)
    {
        Id = id.NotNullOrEmpty();
        Depth = (parentIteration?.Depth ?? 0) + 1;
        BranchName = branchName.NotNull();
        Configuration = configuration.NotNull();
        ParentIteration = parentIteration;
        BranchedFrom = branchedFrom;
        BranchedTo = branchedTo;
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

    internal IEnumerable<MainlineCommit> GetCommitsInReverseOrder(
        bool includeAncestry, MainlineCommit? tip = null)
    {
        var commit = tip ?? Commits.LastOrDefault();
        while (commit is not null)
        {
            yield return commit;
            commit = commit.Predecessor;
        }

        if (includeAncestry && GetBranchedFrom() is { } branchedFrom)
        {
            foreach (var ancestor in branchedFrom.Iteration.GetCommitsInReverseOrder(true, branchedFrom))
            {
                yield return ancestor;
            }
        }
    }
}
