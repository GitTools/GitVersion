namespace GitVersion
{
    public interface IGitVersionExecutor
    {
        int Execute(GitVersionOptions gitVersionOptions);
    }
}
