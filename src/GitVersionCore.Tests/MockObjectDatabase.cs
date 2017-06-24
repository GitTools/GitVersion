using LibGit2Sharp;

public class MockObjectDatabase : ObjectDatabase
{
    public override Commit FindMergeBase(Commit first, Commit second)
    {
        return second;
    }
}