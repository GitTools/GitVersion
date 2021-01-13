using System;
using System.Collections.Generic;
using System.Diagnostics;
using GitVersion;
using LibGit2Sharp;
using NSubstitute;

namespace GitVersionCore.Tests.Mocks
{
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    internal class MockCommit : ICommit
    {
        private static int commitCount = 1;
        private static DateTimeOffset when = DateTimeOffset.Now;

        public MockCommit()
        {
            idEx = Substitute.For<IObjectId>();
            idEx.Sha.Returns(Guid.NewGuid().ToString().Replace("-", "") + "00000000");
            MessageEx = "Commit " + commitCount++;
            ParentsEx = new List<ICommit> { null };
            CommitterEx = new Signature("Joe", "Joe@bloggs.net", when);
            // Make sure each commit is a different time
            when = when.AddSeconds(1);
        }

        public string MessageEx;
        public string Message => MessageEx;

        public Signature CommitterEx;
        public DateTimeOffset? CommitterWhen => CommitterEx.When;

        private readonly IObjectId idEx;
        public IObjectId Id => idEx;

        public string Sha => idEx.Sha;

        public IList<ICommit> ParentsEx;
        public IEnumerable<ICommit> Parents => ParentsEx;

        // ReSharper disable once UnusedMember.Local
        private string DebuggerDisplay => MessageEx;
        public bool Equals(ICommit other)
        {
            throw new NotImplementedException();
        }
        public int CompareTo(ICommit other)
        {
            throw new NotImplementedException();
        }
    }
}
