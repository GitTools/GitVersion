using System;
using System.Collections.Generic;
using System.Linq;

namespace GitVersion.VersionCalculation
{
    public class ShaVersionFilter : IVersionFilter
    {
        private readonly IEnumerable<string> shas;

        public ShaVersionFilter(IEnumerable<string> shas)
        {
            this.shas = shas ?? throw new ArgumentNullException(nameof(shas));
        }

        public bool Exclude(BaseVersion version, out string reason)
        {
            if (version == null) throw new ArgumentNullException(nameof(version));

            reason = null;

            if (version.BaseVersionSource != null &&
                shas.Any(sha => version.BaseVersionSource.Sha.StartsWith(sha, StringComparison.OrdinalIgnoreCase)))
            {
                reason = $"Sha {version.BaseVersionSource.Sha} was ignored due to commit having been excluded by configuration";
                return true;
            }

            return false;
        }
    }
}
