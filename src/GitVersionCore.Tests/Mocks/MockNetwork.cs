namespace GitVersionCore.Tests.Mocks
{
    using System.Collections.Generic;

    using LibGit2Sharp;

    public class MockNetwork : Network
    {
        public MockNetwork(IEnumerable<MockRemote> mockRemotes)
        {
            Remotes = new MockRemoteCollection(mockRemotes);
        }

        public override RemoteCollection Remotes { get; }
    }
}
