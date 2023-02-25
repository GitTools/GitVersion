using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

public class IgnoreConfiguration
{
    public IgnoreConfiguration() => Shas = Array.Empty<string>();

    [JsonPropertyName("commits-before")]
    public DateTimeOffset? Before { get; set; }

    [JsonPropertyName("sha")]
    public string[] Shas { get; set; }

    [JsonIgnore]
    public virtual bool IsEmpty => Before == null && Shas.Any() == false;

    public virtual IEnumerable<IVersionFilter> ToFilters()
    {
        if (Shas.Any()) yield return new ShaVersionFilter(Shas);
        if (Before.HasValue) yield return new MinDateVersionFilter(Before.Value);
    }
}
