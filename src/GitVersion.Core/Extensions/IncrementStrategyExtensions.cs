namespace GitVersion.Extensions;

/// <summary>Extension methods on <see cref="IncrementStrategy"/> for converting to related types.</summary>
public static class IncrementStrategyExtensions
{
    extension(IncrementStrategy strategy)
    {
        /// <summary>Converts the <see cref="IncrementStrategy"/> to the equivalent <see cref="VersionField"/>.</summary>
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
