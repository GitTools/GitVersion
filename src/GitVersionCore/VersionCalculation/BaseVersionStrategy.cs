namespace GitVersion.VersionCalculation
{
    using System.Collections.Generic;
    using BaseVersionCalculators;

    public abstract class BaseVersionStrategy
    {
        /// <summary>
        /// Calculates the <see cref="BaseVersion"/> values for the given <paramref name="context"/>.
        /// </summary>
        /// <param name="context">
        /// The context for calculating the <see cref="BaseVersion"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{BaseVersion}"/> of the base version values found by the strategy.
        /// </returns>
        public abstract IEnumerable<BaseVersion> GetVersions(GitVersionContext context);
    }
}