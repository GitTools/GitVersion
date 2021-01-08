namespace GitVersion
{
    public interface IBranch
    {
        string CanonicalName { get; }
        string FriendlyName { get; }
        Commit Tip { get; }
        bool IsRemote { get; }
        bool IsTracking { get; }
        CommitCollection Commits { get; }
    }
}
