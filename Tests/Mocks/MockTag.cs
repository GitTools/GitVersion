using LibGit2Sharp;

public class MockTag:Tag
{
    public MockTag()
    {
    }

    public string NameEx;
    public override string Name
    {
        get { return NameEx; }
    }

    public GitObject TargetEx;
    public override GitObject Target
    {
        get { return TargetEx; }
    }
    public TagAnnotation AnnotationEx;
    public override TagAnnotation Annotation
    {
        get { return AnnotationEx; }
    }

}
public class MockTagAnnotation:TagAnnotation
{
    public MockTagAnnotation()
    {
    }

    public Signature TaggerEx;
    public override Signature Tagger
    {
        get { return TaggerEx; }
    }
}