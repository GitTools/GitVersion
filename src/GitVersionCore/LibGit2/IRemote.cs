namespace GitVersion
{
    public interface IRemote
    {
        string Name { get; }
        string RefSpecs { get; }
    }
}
