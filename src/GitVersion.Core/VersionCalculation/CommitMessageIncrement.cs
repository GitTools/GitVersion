namespace GitVersion.VersionCalculation;

/// <summary>Represents a version increment selected from a commit message and whether prior increments must be reset.</summary>
/// <param name="Increment">The version field selected by the commit message.</param>
/// <param name="VersionBumpNeedsToBeReset">Whether increments accumulated before this commit must be discarded.</param>
public readonly record struct CommitMessageIncrement(VersionField Increment, bool VersionBumpNeedsToBeReset);
