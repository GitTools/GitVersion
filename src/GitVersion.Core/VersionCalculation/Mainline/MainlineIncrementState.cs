namespace GitVersion.VersionCalculation.Mainline;

// Projects the already emitted increments without replaying the whole prefix at each commit.
internal sealed class MainlineIncrementState
{
    internal BaseVersion? Version { get; private set; }

    internal SemanticVersion? ReleaseBaseline { get; private set; }

    internal void Add(IBaseVersionIncrement increment)
    {
        if (increment is BaseVersionOperand operand)
        {
            ReleaseBaseline = operand.SemanticVersion;
        }
        Version = Apply(Version, increment);
    }

    internal static BaseVersion? Apply(BaseVersion? version, IBaseVersionIncrement increment)
        => increment switch
        {
            BaseVersionOperand operand => new BaseVersion(operand),
            BaseVersionOperator operation => (version ?? new BaseVersion()).Apply(operation),
            BaseVersion resolved => resolved,
            _ => version
        };
}
