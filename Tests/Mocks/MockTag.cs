using LibGit2Sharp;

public class MockTag:Tag
{
    public MockTag()
    {
    }

    public string NameEx { get; set; }
    public override string Name
    {
        get { return NameEx; }
    }

    public GitObject TargetEx { get; set; }
    public override GitObject Target
    {
        get { return TargetEx; }
    }

}