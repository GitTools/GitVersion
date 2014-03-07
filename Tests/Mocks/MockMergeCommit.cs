using LibGit2Sharp;

public class MockMergeCommit : MockCommit
{
    public MockMergeCommit(ObjectId id = null) : base(id)
    {
        ParentsEx.Add(null);
    }
}
