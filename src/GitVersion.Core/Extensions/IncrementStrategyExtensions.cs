namespace GitVersion.Extensions;

public static class IncrementStrategyExtensions
{
    extension(IncrementStrategy strategy)
    {
        public VersionField ToVersionField() => strategy switch
        {
            IncrementStrategy.None => VersionField.None,
            IncrementStrategy.Major => VersionField.Major,
            IncrementStrategy.Minor => VersionField.Minor,
            IncrementStrategy.Patch => VersionField.Patch,
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null)
        };
    }
}
