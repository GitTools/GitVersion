namespace GitVersion;

/// <summary>Creates a <see cref="GitVersionContext"/> for the current repository and branch.</summary>
public interface IGitVersionContextFactory
{
    /// <summary>Builds and returns the <see cref="GitVersionContext"/> for the current execution.</summary>
    GitVersionContext Create();
}
