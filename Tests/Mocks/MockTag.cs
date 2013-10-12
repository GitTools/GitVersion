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

}