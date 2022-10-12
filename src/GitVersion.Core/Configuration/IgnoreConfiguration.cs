using GitVersion.VersionCalculation;
using YamlDotNet.Serialization;

namespace GitVersion.Configuration;

public class IgnoreConfiguration
{
    public IgnoreConfiguration() => ShAs = Enumerable.Empty<string>();

    [YamlMember(Alias = "commits-before")]
    public DateTimeOffset? Before { get; set; }

    [YamlMember(Alias = "sha")]
    public IEnumerable<string> ShAs { get; set; }

    [YamlIgnore]
    public virtual bool IsEmpty => Before == null && ShAs.Any() == false;

    public virtual IEnumerable<IVersionFilter> ToFilters()
    {
        if (ShAs.Any()) yield return new ShaVersionFilter(ShAs);
        if (Before.HasValue) yield return new MinDateVersionFilter(Before.Value);
    }
}
