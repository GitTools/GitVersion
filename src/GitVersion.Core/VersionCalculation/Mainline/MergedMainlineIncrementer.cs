using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.Mainline;

// Resolves merge policy while producing ordinary increments, before the final application step.
internal sealed class MergedMainlineIncrementer(IIncrementer incrementer) : IIncrementer
{
    internal static IReadOnlyCollection<IIncrementer> Decorate(IReadOnlyCollection<IIncrementer> incrementers)
        => [.. incrementers.Select(element => new MergedMainlineIncrementer(element))];

    public bool MatchPrecondition(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
        => incrementer.MatchPrecondition(iteration, commit, context);

    public IEnumerable<IBaseVersionIncrement> GetIncrements(
        MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        foreach (var item in incrementer.GetIncrements(iteration, commit, context))
        {
            yield return item is BaseVersionOperator operation && context.IsMainlineMerge
                && context.SemanticVersion is null && commit.ChildIteration is { } child
                && !context.GetEffectiveConfiguration(child).PreventIncrementWhenBranchMerged
                ? Resolve(context.Increments.Version ?? new BaseVersion(), operation,
                    GetSourceVersion(child, context), context.Increments.ReleaseBaseline)
                : item;
        }
    }

    internal static BaseVersion GetSourceVersion(MainlineIteration iteration, MainlineContext context)
    {
        // Inherited configurations depend on their enclosing iteration.
        var canCache = iteration.Configuration.Increment != IncrementStrategy.Inherit;
        if (canCache && context.MainlineVersions.TryGetValue(iteration, out var cachedVersion))
        {
            return cachedVersion;
        }
        var version = CalculateSourceVersion(iteration, context);
        if (canCache)
        {
            context.MainlineVersions.Add(iteration, version);
        }
        return version;
    }

    private static BaseVersion CalculateSourceVersion(MainlineIteration iteration, MainlineContext context)
    {
        // An ignored source tip must not be mistaken for its earlier released ancestor.
        var tip = iteration.BranchedTo.NotNull().Parents[1];
        var configuration = context.GetEffectiveConfiguration(iteration);
        var label = configuration.GetBranchSpecificLabel(iteration.BranchName, null, context.Environment, tip);
        var sourceContext = context.CreateChildContext() with
        {
            TargetLabel = label,
            IncludeSharedHistory = true
        };
        var version = context.Calculator.CalculateBaseVersion(iteration, sourceContext, includeAncestry: true);
        // A released source tip contributes its tag, not its next pending increment.
        if (version.Operand.BaseVersionSource?.Equals(tip) == true
            && configuration.PreventIncrementWhenCurrentCommitTagged
            && version.Operator is { } operation)
        {
            version = version with { Operator = operation with { Increment = VersionField.None } };
        }
        return version;
    }

    internal static BaseVersion Resolve(BaseVersion receiver, BaseVersionOperator operation, BaseVersion source,
        SemanticVersion? baseline)
    {
        var receiverVersion = receiver.GetIncrementedVersion();
        var continuesReleasedPrerelease = baseline is { IsPreRelease: true }
            && baseline.Major == receiverVersion.Major && baseline.Minor == receiverVersion.Minor
            && baseline.Patch == receiverVersion.Patch;
        // Generated unnamed suffixes restart; an unchanged released prerelease keeps its sequence.
        if (receiverVersion.PreReleaseTag.Name.IsNullOrEmpty() && !continuesReleasedPrerelease)
        {
            receiverVersion = new SemanticVersion(receiverVersion)
            {
                PreReleaseTag = SemanticVersionPreReleaseTag.Empty
            };
        }
        return new BaseVersion(receiver.Source, receiverVersion, receiver.BaseVersionSource)
        {
            SemVerSource = receiver.GetSemVerSource()
        }.Apply(WithSourceVersion(operation, source.GetIncrementedVersion(), source.GetSemVerSource()));
    }

    private static BaseVersionOperator WithSourceVersion(BaseVersionOperator operation, SemanticVersion version,
        SemanticVersionSource? source)
        => operation.AlternativeSemanticVersion is { } alternative
            && !alternative.IsLessThan(version, includePreRelease: false)
            ? operation
            : operation with { AlternativeSemanticVersion = version, AlternativeSemVerSource = source };
}
