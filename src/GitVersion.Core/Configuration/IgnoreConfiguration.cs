using GitVersion.VersionCalculation;
using YamlDotNet.Serialization;

namespace GitVersion.Configuration;

public class IgnoreConfiguration
{
    public IgnoreConfiguration() => Shas = Enumerable.Empty<string>();

    [YamlMember(Alias = "commits-before")]
    public DateTimeOffset? Before { get; set; }

    [YamlMember(Alias = "sha")]
    public IEnumerable<string> Shas { get; set; }

    [YamlIgnore]
    public virtual bool IsEmpty => Before == null && Shas.Any() == false;

    public virtual IEnumerable<IVersionFilter> ToFilters()
    {
        if (Shas.Any()) yield return new ShaVersionFilter(Shas);
        if (Before.HasValue) yield return new MinDateVersionFilter(Before.Value);
    }
}
