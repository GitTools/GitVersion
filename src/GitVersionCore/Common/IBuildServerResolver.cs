namespace GitVersion
{
    public interface IBuildServerResolver
    {
        IBuildServer Resolve();
    }
}
