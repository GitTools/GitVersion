namespace GitVersion
{
    public interface IGitVersionExecutor
    {
        int Run(Arguments arguments);
    }
}