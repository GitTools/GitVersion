namespace GitVersion
{
    public interface ITag
    {
        string TargetSha { get; }
        string FriendlyName { get; }
        string CanonicalName { get; }
        ICommit PeeledTargetCommit();
    }
}
