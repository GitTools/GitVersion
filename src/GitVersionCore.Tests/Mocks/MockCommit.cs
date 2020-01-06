using System;
using System.Collections.Generic;
using System.Diagnostics;
using LibGit2Sharp;

namespace GitVersionCore.Tests.Mocks
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class MockCommit : Commit
    {
        private static int commitCount = 1;
        private static DateTimeOffset when = DateTimeOffset.Now;

        public MockCommit(ObjectId id = null)
        {
            idEx = id ?? new ObjectId(Guid.NewGuid().ToString().Replace("-", "") + "00000000");
            MessageEx = "Commit " + commitCount++;
            ParentsEx = new List<Commit> { null };
            CommitterEx = new Signature("Joe", "Joe@bloggs.net", when);
            // Make sure each commit is a different time
            when = when.AddSeconds(1);
        }

        public string MessageEx;
        public override string Message => MessageEx;

        public Signature CommitterEx;
        public override Signature Committer => CommitterEx;

        private readonly ObjectId idEx;
        public override ObjectId Id => idEx;

        public override string Sha => idEx.Sha;

        public IList<Commit> ParentsEx;
        public override IEnumerable<Commit> Parents => ParentsEx;

        // ReSharper disable once UnusedMember.Local
        private string DebuggerDisplay => MessageEx;
    }
}
