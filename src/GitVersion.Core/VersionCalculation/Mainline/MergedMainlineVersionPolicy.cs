using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation.Mainline;

internal sealed class MergedMainlineVersionPolicy(
    MainlineConfigurationResolver configurationResolver, IEnvironment environment, MainlineVersionCalculator calculator)
{
    internal IBaseVersionIncrement CreateIncrement(
        BaseVersionOperator operation, MainlineCommit commit, MainlineContext context)
        => context.SemanticVersion is null && commit.ChildIteration is { } child
            && !context.ChildConfiguration.NotNull().PreventIncrementWhenBranchMerged
            ? Resolve(context.Increments.Version ?? new BaseVersion(), operation,
                GetSourceVersion(child, context), context.Increments.ReleaseBaseline)
            : operation;

    internal BaseVersion GetSourceVersion(MainlineIteration iteration, MainlineContext context)
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

    private BaseVersion CalculateSourceVersion(MainlineIteration iteration, MainlineContext context)
    {
        // An ignored source tip must not be mistaken for its earlier released ancestor.
        var merge = iteration.BranchedTo.NotNull();
        var tip = merge.Parents[iteration.Configuration.IsMainBranch != true
            && iteration.ParentIteration is { } parent && iteration.BranchName == parent.BranchName ? 0 : 1];
        var configuration = configurationResolver.GetEffectiveConfiguration(iteration);
        var label = configuration.GetBranchSpecificLabel(iteration.BranchName, null, environment, tip);
        var sourceContext = context.CreateChildContext() with
        {
            TargetLabel = label,
            IncludeSharedHistory = true
        };
        var version = calculator.CalculateBaseVersion(iteration, sourceContext, includeAncestry: true);
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
