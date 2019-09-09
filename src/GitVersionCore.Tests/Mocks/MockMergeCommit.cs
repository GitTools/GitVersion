using LibGit2Sharp;

namespace GitVersionCore.Tests.Mocks
{
    public class MockMergeCommit : MockCommit
    {
        public MockMergeCommit(ObjectId id = null) : base(id)
        {
            ParentsEx.Add(null);
        }
    }
}