using LibGit2Sharp;

public class MockRemote : Remote
{
    public MockRemote(string name) => Name = name;

    public override string Name { get; }
}
