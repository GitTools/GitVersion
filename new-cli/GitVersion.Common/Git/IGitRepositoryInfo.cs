namespace GitVersion;

public interface IGitRepositoryInfo
{
    string? DotGitDirectory { get; }
    string? ProjectRootDirectory { get; }
    string? DynamicGitRepositoryPath { get; }
    string? GitRootPath { get; }
}
