using System.Collections;
using System.Collections.Generic;
using LibGit2Sharp;

public class MockBranch : Branch, ICollection<Commit>
{
    public MockBranch(string name)
    {
        this.name = name;
    }
    public MockBranch()
    {
        
    }
    MockCommitLog commits = new MockCommitLog();
    string name;
    public override string Name { get { return name; } }
    public override ICommitLog Commits { get { return commits; } }

    public IEnumerator<Commit> GetEnumerator()
    {
        return commits.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(Commit item)
    {
        commits.Add(item);
    }

    public void Clear()
    {
        commits.Clear();
    }

    public bool Contains(Commit item)
    {
        return commits.Contains(item);
    }

    public void CopyTo(Commit[] array, int arrayIndex)
    {
        commits.CopyTo(array, arrayIndex);
    }

    public bool Remove(Commit item)
    {
        return commits.Remove(item);
    }

    public int Count{get{return commits.Count;}}

    public bool IsReadOnly { get{return false;} }
}