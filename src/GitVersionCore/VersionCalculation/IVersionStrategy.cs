using System.Collections.Generic;
using GitVersion.VersionCalculation.BaseVersionCalculators;

namespace GitVersion.VersionCalculation
{
    public interface IVersionStrategy
    {
        /// <summary>
        /// Calculates the <see cref="T:GitVersion.VersionCalculation.BaseVersionCalculators.BaseVersion" /> values for the given <paramref name="context" />.
        /// </summary>
        /// <param name="context">
        /// The context for calculating the <see cref="T:GitVersion.VersionCalculation.BaseVersionCalculators.BaseVersion" />.
        /// </param>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of the base version values found by the strategy.
        /// </returns>
        IEnumerable<BaseVersion> GetVersions(GitVersionContext context);
    }
}
