namespace GitVersion
{
    public interface ITag
    {
        string TargetSha { get; }
        string FriendlyName { get; }
        Commit PeeledTargetCommit();
    }
}
