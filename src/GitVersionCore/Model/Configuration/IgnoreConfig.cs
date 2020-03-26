using System;
using System.Collections.Generic;
using System.Linq;
using GitVersion.VersionCalculation;
using YamlDotNet.Serialization;

namespace GitVersion.Model.Configuration
{
    public class IgnoreConfig
    {
        public IgnoreConfig()
        {
            ShAs = Enumerable.Empty<string>();
        }

        [YamlMember(Alias = "commits-before")]
        public DateTimeOffset? Before { get; set; }

        [YamlMember(Alias = "sha")]
        public IEnumerable<string> ShAs { get; set; }

        public virtual IEnumerable<IVersionFilter> ToFilters()
        {
            if (ShAs.Any()) yield return new ShaVersionFilter(ShAs);
            if (Before.HasValue) yield return new MinDateVersionFilter(Before.Value);
        }
    }
}
