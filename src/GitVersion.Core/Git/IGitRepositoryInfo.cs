namespace GitVersion.Git;

/// <summary>Provides paths describing the layout of the Git repository on disk.</summary>
public interface IGitRepositoryInfo
{
    /// <summary>Gets the path to the <c>.git</c> directory, or <see langword="null"/> when not yet discovered.</summary>
    string? DotGitDirectory { get; }

    /// <summary>Gets the path to the project root directory (the directory that contains the solution or project file), or <see langword="null"/> when not determined.</summary>
    string? ProjectRootDirectory { get; }

    /// <summary>Gets the path to the dynamically created Git repository used for shallow-clone scenarios, or <see langword="null"/> when not applicable.</summary>
    string? DynamicGitRepositoryPath { get; }

    /// <summary>Gets the root path of the Git working tree, or <see langword="null"/> when not yet discovered.</summary>
    string? GitRootPath { get; }
}
