using GitVersion.Attributes;
using GitVersion.VersionCalculation;

namespace GitVersion.Configuration;

public record IgnoreConfiguration : IIgnoreConfiguration
{
    [JsonPropertyName("commits-before")]
    [JsonPropertyDescription("Commits before this date will be ignored. Format: yyyy-MM-ddTHH:mm:ss.")]
    [JsonPropertyPattern("'yyyy-MM-ddTHH:mm:ss'", PatternFormat.DateTime)]
    public DateTimeOffset? Before { get; init; }

    [JsonIgnore]
    IReadOnlyList<string> IIgnoreConfiguration.Shas => Shas;

    [JsonPropertyName("sha")]
    [JsonPropertyDescription("A sequence of SHAs to be excluded from the version calculations.")]
    public List<string> Shas { get; init; } = new();

    [JsonIgnore]
    public virtual bool IsEmpty => Before == null && !Shas.Any();

    public virtual IEnumerable<IVersionFilter> ToFilters()
    {
        if (Shas.Any()) yield return new ShaVersionFilter(Shas);
        if (Before.HasValue) yield return new MinDateVersionFilter(Before.Value);
    }
}
