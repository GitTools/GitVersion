using LibGit2Sharp;

public class MockTag : Tag
{

    public string NameEx;
    public override string FriendlyName => NameEx;

    public GitObject TargetEx;
    public override GitObject Target => TargetEx;
    public TagAnnotation AnnotationEx;

    public MockTag() { }

    public MockTag(string name, Commit target)
    {
        NameEx = name;
        TargetEx = target;
    }

    public override TagAnnotation Annotation => AnnotationEx;
}