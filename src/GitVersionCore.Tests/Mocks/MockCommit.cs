using System;
using System.Collections.Generic;
using System.Diagnostics;
using GitVersion;
using LibGit2Sharp;
using Commit = GitVersion.Commit;
using ObjectId = GitVersion.ObjectId;

namespace GitVersionCore.Tests.Mocks
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    internal class MockCommit : Commit
    {
        private static int commitCount = 1;
        private static DateTimeOffset when = DateTimeOffset.Now;

        public MockCommit(IObjectId id = null)
        {
            idEx = id ?? new ObjectId(Guid.NewGuid().ToString().Replace("-", "") + "00000000");
            MessageEx = "Commit " + commitCount++;
            ParentsEx = new List<ICommit> { null };
            CommitterEx = new Signature("Joe", "Joe@bloggs.net", when);
            // Make sure each commit is a different time
            when = when.AddSeconds(1);
        }

        public string MessageEx;
        public override string Message => MessageEx;

        public Signature CommitterEx;
        public override DateTimeOffset? CommitterWhen => CommitterEx.When;

        private readonly IObjectId idEx;
        public override IObjectId Id => idEx;

        public override string Sha => idEx.Sha;

        public IList<ICommit> ParentsEx;
        public override IEnumerable<ICommit> Parents => ParentsEx;

        // ReSharper disable once UnusedMember.Local
        private string DebuggerDisplay => MessageEx;
    }
}
