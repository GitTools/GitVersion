using GitVersion.Configuration.Attributes;

namespace GitVersion.Configuration;

internal class PreventIncrementConfiguration : IPreventIncrementConfiguration
{
    [JsonPropertyName("of-merged-branch")]
    [JsonPropertyDescription("Prevent increment when branch merged.")]
    public bool? OfMergedBranch { get; internal set; }

    [JsonPropertyName("when-branch-merged")]
    [JsonPropertyDescription("Prevent increment when branch merged.")]
    public bool? WhenBranchMerged { get; internal set; }

    [JsonPropertyName("when-current-commit-tagged")]
    [JsonPropertyDescription("This branch related property controls the behavior whether to use the tagged (value set to true) or the incremented (value set to false) semantic version. Defaults to true.")]
    public bool? WhenCurrentCommitTagged { get; internal set; }
}
