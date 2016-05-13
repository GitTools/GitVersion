using System;
using GitVersion.VersionCalculation.BaseVersionCalculators;

namespace GitVersion.VersionFilters
{
    public class MinDateVersionFilter : IVersionFilter
    {
        private readonly DateTimeOffset minimum;

        public MinDateVersionFilter(DateTimeOffset minimum)
        {
            this.minimum = minimum;
        }

        public bool Exclude(BaseVersion version, out string reason)
        {
            if (version == null) throw new ArgumentNullException("version");

            reason = null;

            if (version.BaseVersionSource != null &&
                version.BaseVersionSource.When() < minimum)
            {
                reason = "Source was ignored due to commit date being outside of configured range";
                return true;
            }

            return false;
        }
    }
}
