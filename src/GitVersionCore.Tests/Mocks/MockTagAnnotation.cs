using LibGit2Sharp;

public class MockTagAnnotation : TagAnnotation
{

    public Signature TaggerEx;
    public override Signature Tagger => TaggerEx;

    public GitObject TargetEx;
    public override GitObject Target => TargetEx;
}