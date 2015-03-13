using System;
using System.Collections.Generic;
using System.Diagnostics;
using LibGit2Sharp;

[DebuggerDisplay("{DebuggerDisplay}")]
public class MockCommit : Commit
{
    static int commitCount = 1;
    static DateTimeOffset when = DateTimeOffset.Now;

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
    public override string Message { get { return MessageEx; } }

    public Signature CommitterEx;
    public override Signature Committer { get { return CommitterEx; } }

    ObjectId idEx;
    public override ObjectId Id { get { return idEx; } }

    public override string Sha { get { return idEx.Sha; } }

    public IList<Commit> ParentsEx;
    public override IEnumerable<Commit> Parents
    {
        get { return ParentsEx; }
    }

    // ReSharper disable once UnusedMember.Local
    string DebuggerDisplay
    {
        get
        {
            return MessageEx;
        }
    }
}