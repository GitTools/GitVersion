using System.Diagnostics.CodeAnalysis;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation.TrunkBased;

[DebuggerDisplay(
    @"\{ BranchName = {" + nameof(BranchName) + "}, Increment = {" + nameof(Increment) + "}, " +
    "HasSuccessor = {" + nameof(HasSuccessor) + "}, HasPredecessor = {" + nameof(HasPredecessor) + "}, " +
    "HasChildIteration = {" + nameof(HasChildIteration) + "}, Message = {" + nameof(Message) + @"} \}"
)]
internal record TrunkBasedCommit(TrunkBasedIteration Iteration, ICommit Value, ReferenceName BranchName, IBranchConfiguration Configuration)
{
    public bool IsPredecessorTheLastCommitOnTrunk(IGitVersionConfiguration configuration)
        => !GetEffectiveConfiguration(configuration).IsMainBranch && Predecessor?.GetEffectiveConfiguration(configuration).IsMainBranch == true;

    public VersionField Increment { get; set; }

    public TrunkBasedIteration Iteration { get; } = Iteration.NotNull();

    public ReferenceName BranchName { get; } = BranchName.NotNull();

    private IBranchConfiguration Configuration { get; } = Configuration.NotNull();

    public bool HasSuccessor => Successor is not null;

    public TrunkBasedCommit? Successor { get; private set; }

    public bool HasPredecessor => Predecessor is not null;

    public TrunkBasedCommit? Predecessor { get; private set; }

    public ICommit Value { get; } = Value.NotNull();

    public string Message => Value.Message;

    public TrunkBasedIteration? ChildIteration { get; private set; }

    [MemberNotNullWhen(true, nameof(ChildIteration))]
    public bool HasChildIteration => ChildIteration is not null && ChildIteration.Commits.Count != 0;

    public TrunkBasedIteration? ParentIteration => Iteration.ParentIteration;

    public TrunkBasedCommit? ParentCommit => Iteration.ParentCommit;

    [MemberNotNullWhen(true, nameof(ParentIteration), nameof(ParentCommit))]
    public bool HasParentIteration => Iteration.ParentIteration is not null && Iteration.ParentCommit is not null;

    public IReadOnlyCollection<SemanticVersion> SemanticVersions => semanticVersions;

    private readonly HashSet<SemanticVersion> semanticVersions = [];

    private EffectiveConfiguration? effectiveConfiguration;

    public EffectiveConfiguration GetEffectiveConfiguration(IGitVersionConfiguration configuration)
    {
        if (effectiveConfiguration is not null) return effectiveConfiguration;

        IBranchConfiguration branchConfiguration = Configuration;

        IBranchConfiguration? last = Configuration;
        for (var i = this; i is not null; i = i.Predecessor)
        {
            if (branchConfiguration.Increment != IncrementStrategy.Inherit) break;

            if (i.Configuration != last)
            {
                branchConfiguration = branchConfiguration.Inherit(i.Configuration);
            }

            last = i.Configuration;
        }

        if (branchConfiguration.Increment == IncrementStrategy.Inherit && HasParentIteration)
        {
            var parentConfiguration = ParentCommit.GetEffectiveConfiguration(configuration);
            branchConfiguration = branchConfiguration.Inherit(parentConfiguration);
        }

        return effectiveConfiguration = new EffectiveConfiguration(configuration, branchConfiguration);
    }

    public VersionField GetIncrementForcedByBranch(IGitVersionConfiguration configuration)
    {
        var result = GetEffectiveConfiguration(configuration);
        return result.Increment.ToVersionField();
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
        ICommit value, ReferenceName branchName, IBranchConfiguration configuration)
    {
        if (HasPredecessor) throw new InvalidOperationException();

        TrunkBasedCommit commit = new(Iteration, value, branchName, configuration);
        Predecessor = commit;
        commit.Successor = this;

        return commit;
    }
}
