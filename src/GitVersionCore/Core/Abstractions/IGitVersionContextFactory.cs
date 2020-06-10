namespace GitVersion
{
    public interface IGitVersionContextFactory
    {
        GitVersionContext Create(GitVersionOptions gitVersionOptions);
    }
}
