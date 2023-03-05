using GitVersion.Attributes;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

public class IgnoreConfiguration
{
    public IgnoreConfiguration() => Shas = Array.Empty<string>();

    [JsonPropertyName("commits-before")]
    [JsonPropertyDescription("Commits before this date will be ignored. Format: yyyy-MM-ddTHH:mm:ss.")]
    [JsonPropertyPattern("'yyyy-MM-ddTHH:mm:ss'", PatternFormat.DateTime)]
    public DateTimeOffset? Before { get; set; }

    [JsonPropertyName("sha")]
    [JsonPropertyDescription("A sequence of SHAs to be excluded from the version calculations.")]
    public string[] Shas { get; set; }

    [JsonIgnore]
    public virtual bool IsEmpty => Before == null && !Shas.Any();

    public virtual IEnumerable<IVersionFilter> ToFilters()
    {
        if (Shas.Any()) yield return new ShaVersionFilter(Shas);
        if (Before.HasValue) yield return new MinDateVersionFilter(Before.Value);
    }
}
