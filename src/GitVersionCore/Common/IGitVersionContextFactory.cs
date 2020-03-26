namespace GitVersion
{
    public interface IGitVersionContextFactory
    {
        GitVersionContext Create(Arguments arguments);
    }
}
