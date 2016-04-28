namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GitVersion.VersionFilters;
    using YamlDotNet.Serialization;

    public class IgnoreConfig
    {
        public IgnoreConfig()
        {
            SHAs = Enumerable.Empty<string>();
        }

        [YamlMember(Alias = "commits-before")]
        public DateTimeOffset? Before { get; set; }

        [YamlMember(Alias = "sha")]
        public IEnumerable<string> SHAs { get; set; }

        public virtual IEnumerable<IVersionFilter> ToFilters()
        {
            if (SHAs.Any()) yield return new ShaVersionFilter(SHAs);
            if (Before.HasValue) yield return new MinDateVersionFilter(Before.Value);
        }
    }
}