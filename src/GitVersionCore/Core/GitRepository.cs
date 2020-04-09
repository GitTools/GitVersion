using System;
using System.Collections.Generic;
using LibGit2Sharp;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    public class GitRepository : IRepository
    {
        private Lazy<IRepository> repositoryLazy;
        private IRepository repositoryInstance => repositoryLazy.Value;

        public GitRepository(IOptions<GitVersionOptions> options)
        {
            repositoryLazy = new Lazy<IRepository>(() => new Repository(options.Value.DotGitDirectory));
        }

        public void Dispose()
        {
            if (repositoryLazy.IsValueCreated)
            {
                repositoryInstance.Dispose();
            }
        }

        public void Checkout(Tree tree, IEnumerable<string> paths, CheckoutOptions opts)
        {
            repositoryInstance.Checkout(tree, paths, opts);
        }

        public void CheckoutPaths(string committishOrBranchSpec, IEnumerable<string> paths, CheckoutOptions checkoutOptions)
        {
            repositoryInstance.CheckoutPaths(committishOrBranchSpec, paths, checkoutOptions);
        }

        public GitObject Lookup(ObjectId id)
        {
            return repositoryInstance.Lookup(id);
        }

        public GitObject Lookup(string objectish)
        {
            return repositoryInstance.Lookup(objectish);
        }

        public GitObject Lookup(ObjectId id, ObjectType type)
        {
            return repositoryInstance.Lookup(id, type);
        }

        public GitObject Lookup(string objectish, ObjectType type)
        {
            return repositoryInstance.Lookup(objectish, type);
        }

        public Commit Commit(string message, Signature author, Signature committer, CommitOptions options)
        {
            return repositoryInstance.Commit(message, author, committer, options);
        }

        public void Reset(ResetMode resetMode, Commit commit)
        {
            repositoryInstance.Reset(resetMode, commit);
        }

        public void Reset(ResetMode resetMode, Commit commit, CheckoutOptions options)
        {
            repositoryInstance.Reset(resetMode, commit, options);
        }

        public void RemoveUntrackedFiles()
        {
            repositoryInstance.RemoveUntrackedFiles();
        }

        public RevertResult Revert(Commit commit, Signature reverter, RevertOptions options)
        {
            return repositoryInstance.Revert(commit, reverter, options);
        }

        public MergeResult Merge(Commit commit, Signature merger, MergeOptions options)
        {
            return repositoryInstance.Merge(commit, merger, options);
        }

        public MergeResult Merge(Branch branch, Signature merger, MergeOptions options)
        {
            return repositoryInstance.Merge(branch, merger, options);
        }

        public MergeResult Merge(string committish, Signature merger, MergeOptions options)
        {
            return repositoryInstance.Merge(committish, merger, options);
        }

        public MergeResult MergeFetchedRefs(Signature merger, MergeOptions options)
        {
            return repositoryInstance.MergeFetchedRefs(merger, options);
        }

        public CherryPickResult CherryPick(Commit commit, Signature committer, CherryPickOptions options)
        {
            return repositoryInstance.CherryPick(commit, committer, options);
        }

        public BlameHunkCollection Blame(string path, BlameOptions options)
        {
            return repositoryInstance.Blame(path, options);
        }

        public FileStatus RetrieveStatus(string filePath)
        {
            return repositoryInstance.RetrieveStatus(filePath);
        }

        public RepositoryStatus RetrieveStatus(StatusOptions options)
        {
            return repositoryInstance.RetrieveStatus(options);
        }

        public string Describe(Commit commit, DescribeOptions options)
        {
            return repositoryInstance.Describe(commit, options);
        }

        public void RevParse(string revision, out Reference reference, out GitObject obj)
        {
            repositoryInstance.RevParse(revision, out reference, out obj);
        }

        public Branch Head => repositoryInstance.Head;

        public LibGit2Sharp.Configuration Config => repositoryInstance.Config;

        public Index Index => repositoryInstance.Index;

        public ReferenceCollection Refs => repositoryInstance.Refs;

        public IQueryableCommitLog Commits => repositoryInstance.Commits;

        public BranchCollection Branches => repositoryInstance.Branches;

        public TagCollection Tags => repositoryInstance.Tags;

        public RepositoryInformation Info => repositoryInstance.Info;

        public Diff Diff => repositoryInstance.Diff;

        public ObjectDatabase ObjectDatabase => repositoryInstance.ObjectDatabase;

        public NoteCollection Notes => repositoryInstance.Notes;

        public SubmoduleCollection Submodules => repositoryInstance.Submodules;

        public WorktreeCollection Worktrees => repositoryInstance.Worktrees;

        public Rebase Rebase => repositoryInstance.Rebase;

        public Ignore Ignore => repositoryInstance.Ignore;

        public Network Network => repositoryInstance.Network;

        public StashCollection Stashes => repositoryInstance.Stashes;
    }
}
