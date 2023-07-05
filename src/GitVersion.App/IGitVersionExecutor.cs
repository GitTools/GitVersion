namespace GitVersion;

internal interface IGitVersionExecutor
{
    int Execute(GitVersionOptions gitVersionOptions);
}
