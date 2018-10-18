using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using GitVersion.VersionCalculation.BaseVersionCalculators;

namespace GitVersion.VersionFilters
{
    public class MessageFilter : IVersionFilter
    {
        private readonly Regex regex;

        public MessageFilter(string regex)
        {
            if (regex == null) throw new ArgumentNullException(nameof(regex));
            this.regex = new Regex(regex); ;
        }

        public bool Exclude(BaseVersion version, out string reason)
        {
            if (version == null) throw new ArgumentNullException(nameof(version));

            reason = null;

            if (version.BaseVersionSource != null &&
                regex.Match(version.BaseVersionSource.Message).Success)
            {
                reason = $"Message {version.BaseVersionSource.Message} was ignored due to commit having been excluded by configuration";
                return true;
            }

            return false;
        }
    }
}
