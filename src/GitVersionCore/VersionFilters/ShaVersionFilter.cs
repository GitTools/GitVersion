using System;
using System.Collections.Generic;
using System.Linq;
using GitVersion.VersionCalculation.BaseVersionCalculators;
using LibGit2Sharp;

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

        public bool Exclude(BaseVersion version, IRepository repository, out string reason)
        {
            if (version == null) throw new ArgumentNullException("version");

            reason = null;

            if (version.Source != null &&
                shas.Any(sha => version.Source.Commit.Sha.StartsWith(sha, StringComparison.OrdinalIgnoreCase)))
            {
                reason = $"Sha {version.Source.Commit.Sha} was ignored due to commit having been excluded by configuration";
                return true;
            }

            return false;
        }
    }
}
