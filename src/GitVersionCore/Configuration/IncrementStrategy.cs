namespace GitVersion
{
    using System;

    public enum IncrementStrategy
    {
        None,
        Major,
        Minor,
        Patch,
        /// <summary>
        /// Uses the increment strategy from the branch the current branch was branched from
        /// </summary>
        Inherit
    }

    public static class IncrementStrategyExtensions
    {
        public static VersionField ToVersionField(this IncrementStrategy strategy)
        {
            switch (strategy)
            {
                case IncrementStrategy.None:
                    return VersionField.None;
                case IncrementStrategy.Major:
                    return VersionField.Major;
                case IncrementStrategy.Minor:
                    return VersionField.Minor;
                case IncrementStrategy.Patch:
                    return VersionField.Patch;
                default:
                    throw new ArgumentOutOfRangeException("strategy", strategy, null);
            }
        }
    }
}