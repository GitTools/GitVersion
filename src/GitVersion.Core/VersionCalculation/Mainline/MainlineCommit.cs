using System.Diagnostics.CodeAnalysis;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation.Mainline;

[DebuggerDisplay(
    @"\{ BranchName = {" + nameof(BranchName) + "}, HasSuccessor = {" + nameof(HasSuccessor) + "}, " +
    "HasPredecessor = {" + nameof(HasPredecessor) + "}, Message = {" + nameof(Message) + @"} \}"
)]
internal sealed class MainlineCommit(MainlineIteration Iteration, ICommit? value, ReferenceName BranchName, IBranchConfiguration Configuration)
{
    public bool HasUnsupportedMerge => !IsDummy && Value.Parents.Count > 2;

    public MainlineIteration Iteration { get; } = Iteration.NotNull();

    public ReferenceName BranchName { get; } = BranchName.NotNull();

    public IBranchConfiguration Configuration { get; } = Configuration.NotNull();

    public bool HasExplicitIncrement => Configuration.Increment != IncrementStrategy.Inherit;

    public bool HasSuccessor => Successor is not null;

    public MainlineCommit? Successor { get; private set; }

    public bool HasPredecessor => Predecessor is not null;

    public MainlineCommit? Predecessor { get; private set; }

    public ICommit Value => IsDummy ? (Successor?.Value)! : value!;

    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsDummy => value is null;

    public string Message => IsDummy ? "<<DUMMY>>" : Value.Message;

    public MainlineIteration? ChildIteration { get; private set; }

    [MemberNotNullWhen(true, nameof(ChildIteration))]
    public bool HasChildIteration => ChildIteration is not null && ChildIteration.Commits.Count != 0;

    public MainlineIteration? ParentIteration => Iteration.ParentIteration;

    public IReadOnlyCollection<SemanticVersion> SemanticVersions => this.semanticVersions;

    private readonly HashSet<SemanticVersion> semanticVersions = [];

    public void AddSemanticVersions(params SemanticVersion[] values)
        => AddSemanticVersions((IEnumerable<SemanticVersion>)values);

    public void AddSemanticVersions(IEnumerable<SemanticVersion> values)
    {
        foreach (var semanticVersion in values.NotNull())
        {
            this.semanticVersions.Add(semanticVersion);
        }
    }

    public void AddChildIteration(MainlineIteration iteration) => ChildIteration = iteration.NotNull();

    public MainlineCommit Append(
        ICommit? referenceValue, ReferenceName branchName, IBranchConfiguration configuration)
    {
        if (HasPredecessor)
        {
            throw new InvalidOperationException("Cannot append to a commit that already has a predecessor.");
        }

        MainlineCommit commit = new(Iteration, referenceValue, branchName, configuration);
        Predecessor = commit;
        commit.Successor = this;

        return commit;
    }
}
