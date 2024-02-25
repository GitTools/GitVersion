namespace GitVersion;

public static class IncrementStrategyExtensions
{
    public static VersionField ToVersionField(this IncrementStrategy strategy) => strategy switch
    {
        IncrementStrategy.None => VersionField.None,
        IncrementStrategy.Major => VersionField.Major,
        IncrementStrategy.Minor => VersionField.Minor,
        IncrementStrategy.Patch => VersionField.Patch,
        _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null)
    };
}
