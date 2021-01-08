namespace GitVersion
{
    public interface IBranch
    {
        string CanonicalName { get; }
        string FriendlyName { get; }
        ICommit Tip { get; }
        bool IsRemote { get; }
        bool IsTracking { get; }
        CommitCollection Commits { get; }
    }
}
