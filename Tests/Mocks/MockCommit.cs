using System;
using System.Diagnostics;
using LibGit2Sharp;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class MockCommit:Commit
{
    public MockCommit()
    {
        idEx = new ObjectId(Guid.NewGuid().ToString().Replace("-", "")+ "00000000");
    }

    public string MessageEx;
    public override string Message{get { return MessageEx; }}

    public Signature CommitterEx;
    public override Signature Committer{get { return CommitterEx; }}

    ObjectId idEx;
    public override ObjectId Id{get { return idEx; }}

    public override string Sha { get { return idEx.Sha; } }
    string DebuggerDisplay
    {
        get
        {
            return MessageEx;
        }
    }
}