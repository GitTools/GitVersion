namespace GitVersion.Configuration;

/// <summary>Represents branch settings that affect rendered version output.</summary>
public interface IOutputBranchConfiguration
{
    /// <summary>Gets the format string used to compute the custom version output on this branch.</summary>
    string? CustomVersionFormat { get; }

    /// <summary>Gets the numeric weight applied to the pre-release tag number to produce a weighted pre-release number.</summary>
    int? PreReleaseWeight { get; }
}
