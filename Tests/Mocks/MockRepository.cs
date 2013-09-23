using System.Collections.Generic;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;

public class MockRepository : IRepository
{

    public MockRepository()
    {
        Tags = new MockTagCollection();
        Refs = new MockReferenceCollection();
    }
    public void Dispose()
    {
        throw new System.NotImplementedException();
    }

    public Branch Checkout(Branch branch, CheckoutModifiers checkoutModifiers, CheckoutProgressHandler onCheckoutProgress, CheckoutNotificationOptions checkoutNotificationOptions)
    {
        throw new System.NotImplementedException();
    }

    public Branch Checkout(string committishOrBranchSpec, CheckoutModifiers checkoutModifiers, CheckoutProgressHandler onCheckoutProgress, CheckoutNotificationOptions checkoutNotificationOptions)
    {
        throw new System.NotImplementedException();
    }

    public Branch Checkout(Commit commit, CheckoutModifiers checkoutModifiers, CheckoutProgressHandler onCheckoutProgress, CheckoutNotificationOptions checkoutNotificationOptions)
    {
        throw new System.NotImplementedException();
    }

    public void CheckoutPaths(string committishOrBranchSpec, IList<string> paths, CheckoutModifiers checkoutOptions, CheckoutProgressHandler onCheckoutProgress, CheckoutNotificationOptions checkoutNotificationOptions)
    {
        throw new System.NotImplementedException();
    }

    public void CheckoutPaths(string committishOrBranchSpec, IEnumerable<string> paths, CheckoutOptions checkoutOptions = null)
    {
        throw new System.NotImplementedException();
    }

    public GitObject Lookup(ObjectId id)
    {
        throw new System.NotImplementedException();
    }

    public GitObject Lookup(string objectish)
    {
        throw new System.NotImplementedException();
    }

    public GitObject Lookup(ObjectId id, ObjectType type)
    {
        throw new System.NotImplementedException();
    }

    public GitObject Lookup(string objectish, ObjectType type)
    {
        throw new System.NotImplementedException();
    }

    public Commit Commit(string message, Signature author, Signature committer, bool amendPreviousCommit = false)
    {
        throw new System.NotImplementedException();
    }

    public void Reset(ResetOptions resetOptions, Commit commit)
    {
        throw new System.NotImplementedException();
    }

    public void Reset(Commit commit, IEnumerable<string> paths = null, ExplicitPathsOptions explicitPathsOptions = null)
    {
        throw new System.NotImplementedException();
    }

    public void RemoveUntrackedFiles()
    {
        throw new System.NotImplementedException();
    }

    public Branch Head { get; set; }
    public Configuration Config { get; private set; }
    public Index Index { get; private set; }
    public ReferenceCollection Refs { get; set; }
    public IQueryableCommitLog Commits { get; set; }
    public BranchCollection Branches { get; set; }
    public TagCollection Tags { get; set; }
    public RepositoryInformation Info { get; private set; }
    public Diff Diff { get; private set; }
    public ObjectDatabase ObjectDatabase { get; private set; }
    public NoteCollection Notes { get; private set; }
    public SubmoduleCollection Submodules { get; private set; }
    public IEnumerable<MergeHead> MergeHeads { get; private set; }
    public Network Network { get; private set; }
}