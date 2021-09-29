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
            PathFilters = new PathFilterConfig();
        }

        [YamlMember(Alias = "commits-before")]
        public DateTimeOffset? Before { get; set; }

        [YamlMember(Alias = "sha")]
        public IEnumerable<string> ShAs { get; set; }

        [YamlIgnore]
        public virtual bool IsEmpty => Before == null
                                       && (ShAs == null || ShAs.Any() == false);

        [YamlMember(Alias = "paths")]
        public PathFilterConfig PathFilters { get; set; }

        public virtual IEnumerable<IVersionFilter> ToFilters()
        {
            if (ShAs.Any()) yield return new ShaVersionFilter(ShAs);
            if (Before.HasValue) yield return new MinDateVersionFilter(Before.Value);
            foreach (var filter in PathFilters.ToFilters())
            {
                yield return filter;
            }
        }
    }
}
