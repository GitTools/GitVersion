using System.Diagnostics;
using LibGit2Sharp;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class MockCommit:Commit
{
    public MockCommit()
    {
    }

    public string MessageEx { get; set; }
    public override string Message{get { return MessageEx; }}

    public Signature CommitterEx { get; set; }
    public override Signature Committer{get { return CommitterEx; }}

    public ObjectId IdEx { get; set; }
    public override ObjectId Id{get { return IdEx; }}

    public string ShaEx { get; set; }
    public override string Sha{get { return ShaEx; }}
    string DebuggerDisplay
    {
        get
        {
            return MessageEx;
        }
    }
}