using System;
using System.Collections.Generic;
using LibGit2Sharp;

public class MockRepository : IRepository
{
    public MockRepository()
    {
        Tags = new MockTagCollection();
        Refs = new MockReferenceCollection();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public Branch Checkout(Branch branch, CheckoutOptions options, Signature signature = null)
    {
        throw new NotImplementedException();
    }

    public Branch Checkout(string committishOrBranchSpec, CheckoutOptions options, Signature signature = null)
    {
        throw new NotImplementedException();
    }

    public Branch Checkout(Commit commit, CheckoutOptions options, Signature signature = null)
    {
        throw new NotImplementedException();
    }

    public void CheckoutPaths(string committishOrBranchSpec, IEnumerable<string> paths, CheckoutOptions checkoutOptions = null)
    {
        throw new NotImplementedException();
    }

    public CherryPickResult CherryPick(Commit commit, Signature committer, CherryPickOptions options = null)
    {
        throw new NotImplementedException();
    }

    public GitObject Lookup(ObjectId id)
    {
        throw new NotImplementedException();
    }

    public GitObject Lookup(string objectish)
    {
        throw new NotImplementedException();
    }

    public GitObject Lookup(ObjectId id, ObjectType type)
    {
        throw new NotImplementedException();
    }

    public GitObject Lookup(string objectish, ObjectType type)
    {
        return new MockCommit();
    }

    public Commit Commit(string message, Signature author, Signature committer, CommitOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Dictionary<string, GitObject> LookupResults { get; set; }

    public Commit Commit(string message, Signature author, Signature committer, bool amendPreviousCommit = false)
    {
        throw new NotImplementedException();
    }

    public void Reset(ResetMode resetMode, Commit commit, Signature signature = null, string logMessage = null)
    {
        throw new NotImplementedException();
    }

    public void Reset(Commit commit, IEnumerable<string> paths = null, ExplicitPathsOptions explicitPathsOptions = null)
    {
        throw new NotImplementedException();
    }

    public void RemoveUntrackedFiles()
    {
        throw new NotImplementedException();
    }

    public RevertResult Revert(Commit commit, Signature reverter, RevertOptions options = null)
    {
        throw new NotImplementedException();
    }

    public MergeResult Merge(Commit commit, Signature merger, MergeOptions options = null)
    {
        throw new NotImplementedException();
    }

    public MergeResult Merge(Branch branch, Signature merger, MergeOptions options = null)
    {
        throw new NotImplementedException();
    }

    public MergeResult Merge(string committish, Signature merger, MergeOptions options = null)
    {
        throw new NotImplementedException();
    }

    public BlameHunkCollection Blame(string path, BlameOptions options = null)
    {
        throw new NotImplementedException();
    }

    public Branch Head { get; set; }
    public Configuration Config { get; set; }
    public Index Index { get; set; }
    public ReferenceCollection Refs { get; set; }
    public IQueryableCommitLog Commits { get; set; }
    public BranchCollection Branches { get; set; }
    public TagCollection Tags { get; set; }
    public RepositoryInformation Info { get; set; }
    public Diff Diff { get; set; }
    public ObjectDatabase ObjectDatabase { get; set; }
    public NoteCollection Notes { get; set; }
    public SubmoduleCollection Submodules { get; set; }

    public Ignore Ignore
    {
        get { throw new NotImplementedException(); }
    }

    public Network Network { get; set; }

    public StashCollection Stashes
    {
        get { throw new NotImplementedException(); }
    }
}