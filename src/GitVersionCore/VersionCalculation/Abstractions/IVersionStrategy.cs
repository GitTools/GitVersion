using System.Collections.Generic;

namespace GitVersion.VersionCalculation
{
    public interface IVersionStrategy
    {
        /// <summary>
        /// Calculates the <see cref="T:GitVersion.VersionCalculation.BaseVersionCalculators.BaseVersion" /> values.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of the base version values found by the strategy.
        /// </returns>
        IEnumerable<BaseVersion> GetVersions();
    }
}
