using System.Diagnostics.CodeAnalysis;
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

    public ICommit? BranchedTo { get; }

    public MainlineCommit? GetBranchedTo()
    {
        if (HasParentIteration)
        {
            return ParentIteration.FindCommit(BranchedTo);
        }
        return null;
    }

    public MainlineIteration? ParentIteration { get; }

    [MemberNotNullWhen(true, nameof(ParentIteration), nameof(BranchedFrom), nameof(BranchedTo))]
    public bool HasParentIteration => ParentIteration is not null;

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
        if (parentIteration is not null)
        {
            if (branchedFrom is null)
            {
                throw new ArgumentException("The parameter cannot be null when parent iteration is specified.", nameof(branchedFrom));
            }
            if (branchedTo is null)
            {
                throw new ArgumentException("The parameter cannot be null when parent iteration is specified.", nameof(branchedTo));
            }
        }
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
}
