using System.Globalization;
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
        set => Before = value is null ? null : DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
    }

    IReadOnlySet<string> IIgnoreConfiguration.Branches => Branches;

    [JsonPropertyName("branches")]
    [JsonPropertyDescription("A sequence of regular expressions matching branch names without the remote prefix to be excluded as version sources. Matching is case-insensitive by default; use (?-i) to enable case-sensitive matching.")]
    public HashSet<string> Branches { get; set; } = [];

    IReadOnlySet<string> IIgnoreConfiguration.Paths => Paths;

    [JsonPropertyName("paths")]
    [JsonPropertyDescription("A sequence of regular expressions matching file paths to be excluded from the version calculations. Matching is case-insensitive by default; use (?-i) to enable case-sensitive matching.")]
    public HashSet<string> Paths { get; set; } = [];

    [JsonIgnore]
    IReadOnlySet<string> IIgnoreConfiguration.Shas => Shas;

    [JsonPropertyName("sha")]
    [JsonPropertyDescription("A sequence of SHAs to be excluded from the version calculations.")]
    public HashSet<string> Shas { get; set; } = [];

    IReadOnlySet<string> IIgnoreConfiguration.Tags => Tags;

    [JsonPropertyName("tags")]
    [JsonPropertyDescription("A sequence of regular expressions matching friendly tag names to be excluded as version sources. Matching is case-insensitive by default; use (?-i) to enable case-sensitive matching.")]
    public HashSet<string> Tags { get; set; } = [];

    [JsonIgnore]
    public bool IsEmpty => Before == null && Branches.Count == 0 && Paths.Count == 0 && Shas.Count == 0 && Tags.Count == 0;
}
