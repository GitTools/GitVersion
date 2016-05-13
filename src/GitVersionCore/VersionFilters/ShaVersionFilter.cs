using System;
using System.Collections.Generic;
using System.Linq;
using GitVersion.VersionCalculation.BaseVersionCalculators;

namespace GitVersion.VersionFilters
{
    public class ShaVersionFilter : IVersionFilter
    {
        private readonly IEnumerable<string> shas;

        public ShaVersionFilter(IEnumerable<string> shas)
        {
            if (shas == null) throw new ArgumentNullException("shas");
            this.shas = shas;
        }

        public bool Exclude(BaseVersion version, out string reason)
        {
            if (version == null) throw new ArgumentNullException("version");

            reason = null;

            if (version.BaseVersionSource != null &&
                shas.Any(sha => version.BaseVersionSource.Sha.StartsWith(sha, StringComparison.OrdinalIgnoreCase)))
            {
                reason = "Source was ignored due to commit having been excluded by configuration";
                return true;
            }

            return false;
        }
    }
}
