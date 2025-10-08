using System.Collections.ObjectModel;
using GitVersion.Configuration.Attributes;

namespace GitVersion.Configuration;

internal record IgnoreConfiguration : IIgnoreConfiguration
{
    [JsonIgnore]
    public DateTimeOffset? Before { get; set; }

    [JsonPropertyName("commits-before")]
    [JsonPropertyDescription("Commits before this date will be ignored. Format: yyyy-MM-ddTHH:mm:ss.")]
    [JsonPropertyFormat(Format.DateTime)]
    public string? BeforeString
    {
        get => Before?.ToString("yyyy-MM-ddTHH:mm:ssZ");
        set => Before = value is null ? null : DateTimeOffset.Parse(value);
    }

    [JsonIgnore]
    IReadOnlySet<string> IIgnoreConfiguration.Shas => Shas;

    [JsonPropertyName("sha")]
    [JsonPropertyDescription("A sequence of SHAs to be excluded from the version calculations.")]
    public HashSet<string> Shas { get; set; } = [];

    IReadOnlyCollection<string> IIgnoreConfiguration.Paths => Paths;

    [JsonPropertyName("paths")]
    [JsonPropertyDescription("A sequence of file paths to be excluded from the version calculations.")]
    public Collection<string> Paths { get; set; } = [];

    [JsonIgnore]
    public bool IsEmpty => Before == null && Shas.Count == 0 && Paths.Count == 0;
}
