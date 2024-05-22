using System.Diagnostics.CodeAnalysis;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.VersionCalculation.Mainline;

[DebuggerDisplay(
    @"\{ BranchName = {" + nameof(BranchName) + "}, Increment = {" + nameof(Increment) + "}, " +
    "HasSuccessor = {" + nameof(HasSuccessor) + "}, HasPredecessor = {" + nameof(HasPredecessor) + "}, " +
    "HasChildIteration = {" + nameof(HasChildIteration) + "}, Message = {" + nameof(Message) + @"} \}"
)]
internal record MainlineCommit(MainlineIteration Iteration, ICommit? value, ReferenceName BranchName, IBranchConfiguration Configuration)
{
    public bool IsPredecessorTheLastCommitOnTrunk(IGitVersionConfiguration configuration)
        => !GetEffectiveConfiguration(configuration).IsMainBranch && Predecessor?.GetEffectiveConfiguration(configuration).IsMainBranch == true;

    public VersionField Increment { get; set; }

    public MainlineIteration Iteration { get; } = Iteration.NotNull();

    public ReferenceName BranchName { get; } = BranchName.NotNull();

    private IBranchConfiguration Configuration { get; } = Configuration.NotNull();

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

    public MainlineCommit? ParentCommit => Iteration.ParentCommit;

    [MemberNotNullWhen(true, nameof(ParentIteration), nameof(ParentCommit))]
    private bool HasParentIteration => Iteration.ParentIteration is not null && Iteration.ParentCommit is not null;

    public IReadOnlyCollection<SemanticVersion> SemanticVersions => semanticVersions;

    private readonly HashSet<SemanticVersion> semanticVersions = [];

    private EffectiveConfiguration? effectiveConfiguration;

    public EffectiveConfiguration GetEffectiveConfiguration(IGitVersionConfiguration configuration)
    {
        if (effectiveConfiguration is not null) return effectiveConfiguration;

        IBranchConfiguration branchConfiguration = Configuration;

        IBranchConfiguration last = Configuration;
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

    public void AddChildIteration(MainlineIteration iteration) => ChildIteration = iteration.NotNull();

    public MainlineCommit Append(
        ICommit? referenceValue, ReferenceName branchName, IBranchConfiguration configuration)
    {
        if (HasPredecessor) throw new InvalidOperationException();

        MainlineCommit commit = new(Iteration, referenceValue, branchName, configuration);
        Predecessor = commit;
        commit.Successor = this;

        return commit;
    }
}
