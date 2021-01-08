namespace GitVersion
{
    public interface ITag
    {
        string TargetSha { get; }
        string FriendlyName { get; }
        ICommit PeeledTargetCommit();
    }
}
