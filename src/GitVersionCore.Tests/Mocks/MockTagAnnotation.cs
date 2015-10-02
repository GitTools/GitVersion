using LibGit2Sharp;

public class MockTagAnnotation : TagAnnotation
{

    public Signature TaggerEx;
    public override Signature Tagger
    {
        get { return TaggerEx; }
    }

    public GitObject TargetEx;
    public override GitObject Target
    {
        get { return TargetEx; }
    }
}