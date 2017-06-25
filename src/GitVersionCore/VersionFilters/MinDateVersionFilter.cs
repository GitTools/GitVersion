using System;
using GitVersion.VersionCalculation.BaseVersionCalculators;
using LibGit2Sharp;

namespace GitVersion.VersionFilters
{
    // TODO Move filters to the metadata builder
    public class MinDateVersionFilter : IVersionFilter
    {
        private readonly DateTimeOffset minimum;

        public MinDateVersionFilter(DateTimeOffset minimum)
        {
            this.minimum = minimum;
        }

        public bool Exclude(BaseVersion version, IRepository repository, out string reason)
        {
            if (version == null) throw new ArgumentNullException("version");

            reason = null;

            if (version.Source != null &&
                version.Source.Commit.When < minimum)
            {
                reason = "Source was ignored due to commit date being outside of configured range";
                return true;
            }

            return false;
        }
    }
}
