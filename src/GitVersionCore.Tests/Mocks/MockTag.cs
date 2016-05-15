using LibGit2Sharp;

public class MockTag : Tag
{

    public string NameEx;
    public override string FriendlyName
    {
        get { return NameEx; }
    }

    public GitObject TargetEx;
    public override GitObject Target
    {
        get { return TargetEx; }
    }
    public TagAnnotation AnnotationEx;

    public MockTag() { }

    public MockTag(string name, Commit target)
    {
        NameEx = name;
        TargetEx = target;
    }

    public override TagAnnotation Annotation
    {
        get { return AnnotationEx; }
    }

}