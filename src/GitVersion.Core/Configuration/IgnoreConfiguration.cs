using GitVersion.Attributes;

namespace GitVersion.Configuration;

public record IgnoreConfiguration : IIgnoreConfiguration
{
    [JsonPropertyName("commits-before")]
    [JsonPropertyDescription("Commits before this date will be ignored. Format: yyyy-MM-ddTHH:mm:ss.")]
    [JsonPropertyPattern("'yyyy-MM-ddTHH:mm:ss'", PatternFormat.DateTime)]
    public DateTimeOffset? Before { get; init; }

    [JsonIgnore]
    IReadOnlyCollection<string> IIgnoreConfiguration.Shas => Shas;

    [JsonPropertyName("sha")]
    [JsonPropertyDescription("A sequence of SHAs to be excluded from the version calculations.")]
    public HashSet<string> Shas { get; init; } = new();
}
