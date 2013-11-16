using System;
using System.Diagnostics;
using LibGit2Sharp;

[DebuggerDisplay("{DebuggerDisplay}")]
public class MockCommit:Commit
{
    public MockCommit()
    {
        idEx = new ObjectId(Guid.NewGuid().ToString().Replace("-", "")+ "00000000");
        MessageEx = "";
    }

    public string MessageEx;
    public override string Message{get { return MessageEx; }}

    public Signature CommitterEx;
    public override Signature Committer{get { return CommitterEx; }}

    ObjectId idEx;
    public override ObjectId Id{get { return idEx; }}

    public override string Sha { get { return idEx.Sha; } }

    // ReSharper disable once UnusedMember.Local
    string DebuggerDisplay
    {
        get
        {
            return MessageEx;
        }
    }
}