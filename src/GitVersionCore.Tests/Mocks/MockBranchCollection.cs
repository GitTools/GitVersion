using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;

public class MockBranchCollection : BranchCollection, ICollection<Branch>
{
    public List<Branch> Branches = new List<Branch>();

    public override IEnumerator<Branch> GetEnumerator()
    {
        return Branches.GetEnumerator();
    }

    public override Branch this[string friendlyName]
    {
        get { return Branches.FirstOrDefault(x => x.FriendlyName == friendlyName); }
    }

    public void Add(Branch item)
    {
        Branches.Add(item);
    }

    public void Clear()
    {
        Branches.Clear();
    }

    public bool Contains(Branch item)
    {
        return Branches.Contains(item);
    }

    public void CopyTo(Branch[] array, int arrayIndex)
    {
        Branches.CopyTo(array, arrayIndex);
    }

    public override void Remove(Branch item)
    {
        Branches.Remove(item);
    }
    bool ICollection<Branch>.Remove(Branch item)
    {
        return Branches.Remove(item);
    }

    public int Count
    {
        get
        {
            return Branches.Count;
        }
    }
    public bool IsReadOnly { get { return false; } }
}