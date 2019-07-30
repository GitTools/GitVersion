using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;

public class MockBranch : Branch, ICollection<Commit>
{
    public MockBranch(string friendlyName)
    {
        this.friendlyName = friendlyName;
        canonicalName = friendlyName;
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
    public override string FriendlyName => friendlyName;
    public override ICommitLog Commits => commits;
    public override Commit Tip => commits.First();
    public override bool IsTracking => true;

    public override string CanonicalName => canonicalName;

    public override int GetHashCode()
    {
        return friendlyName.GetHashCode();
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

    public int Count => commits.Count;

    public bool IsReadOnly => false;
}
