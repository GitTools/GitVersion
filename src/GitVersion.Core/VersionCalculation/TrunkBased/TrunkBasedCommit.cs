using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.TrunkBased;

[DebuggerDisplay(
    @"\{ BranchName = {" + nameof(BranchName) + "}, Increment = {" + nameof(Increment) + "}, " +
    "HasSuccessor = {" + nameof(HasSuccessor) + "}, HasPredecessor = {" + nameof(HasPredecessor) + "}, " +
    "HasChildIteration = {" + nameof(HasChildIteration) + "}, Message = {" + nameof(Message) + @"} \}"
)]
internal record class TrunkBasedCommit(TrunkBasedIteration Iteration, ICommit Value, ReferenceName BranchName, EffectiveConfiguration Configuration, VersionField Increment)
{
    public bool IsPredecessorTheLastCommitOnTrunk
        => !Configuration.IsMainline && Predecessor?.Configuration.IsMainline == true;

    public TrunkBasedIteration Iteration { get; } = Iteration.NotNull();

    public ReferenceName BranchName { get; } = BranchName.NotNull();

    public EffectiveConfiguration Configuration { get; } = Configuration.NotNull();

    public bool HasSuccessor => Successor is not null;

    public TrunkBasedCommit? Successor { get; private set; }

    public bool HasPredecessor => Predecessor is not null;

    public TrunkBasedCommit? Predecessor { get; private set; }

    public ICommit Value { get; } = Value.NotNull();

    public string Message => Value.Message;

    public TrunkBasedIteration? ChildIteration { get; private set; }

    public bool HasChildIteration => ChildIteration is not null;

    public IReadOnlyCollection<SemanticVersion> SemanticVersions => semanticVersions;

    private readonly HashSet<SemanticVersion> semanticVersions = [];

    public VersionField GetIncrementForcedByBranch()
    {
        ICommit lastCommit = null!;
        for (var i = this; i is not null; i = i.Predecessor)
        {
            if (i.Configuration.Increment != IncrementStrategy.Inherit)
                return i.Configuration.Increment.ToVersionField();
            lastCommit = i.Value;
        }

        if (Iteration.Parent is not null && lastCommit.Parents.FirstOrDefault() is ICommit commit)
        {
            var trunkCommit = Iteration.Parent.FindCommit(commit);
            if (trunkCommit is not null)
                return trunkCommit.GetIncrementForcedByBranch();
        }

        for (var i = Iteration; i is not null; i = i.Parent)
        {
            if (i.Configuration.Increment != IncrementStrategy.Inherit)
                return i.Configuration.Increment.ToVersionField();
        }

        return VersionField.None;
    }

    public void AddSemanticVersions(params SemanticVersion[] values)
        => AddSemanticVersions((IEnumerable<SemanticVersion>)values);

    public void AddSemanticVersions(IEnumerable<SemanticVersion> values)
    {
        foreach (var semanticVersion in values.NotNull())
        {
            semanticVersions.Add(semanticVersion);
        }
    }

    public void AddChildIteration(TrunkBasedIteration iteration) => ChildIteration = iteration.NotNull();

    public TrunkBasedCommit Append(
        ICommit value, ReferenceName branchName, EffectiveConfiguration configuration, VersionField increment)
    {
        if (HasPredecessor) throw new InvalidOperationException();

        TrunkBasedCommit commit = new(Iteration, value, branchName, configuration, increment);
        Predecessor = commit;
        commit.Successor = this;

        return commit;
    }
}
