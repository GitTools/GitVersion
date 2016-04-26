using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;

public class MockBranch : Branch, ICollection<Commit>
{
    public MockBranch(string friendlyName)
    {
        this.friendlyName = friendlyName;
        this.canonicalName = friendlyName;
    }
    public MockBranch(string friendlyName, string canonicalName)
    {
        this.friendlyName = friendlyName;
        this.canonicalName = canonicalName;
    }

    public MockBranch()
    {

    }
    MockCommitLog commits = new MockCommitLog();
    string friendlyName;
    string canonicalName;
    public override string FriendlyName { get { return friendlyName; } }
    public override ICommitLog Commits { get { return commits; } }
    public override Commit Tip { get { return commits.First(); } }
    public override bool IsTracking { get { return true; } }

    public override string CanonicalName
    {
        get { return canonicalName; }
    }

    public override int GetHashCode()
    {
        return this.friendlyName.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return ReferenceEquals(this, obj);
    }

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

    public int Count { get { return commits.Count; } }

    public bool IsReadOnly { get { return false; } }
}