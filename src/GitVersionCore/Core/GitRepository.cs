using System;
using System.Linq;
using LibGit2Sharp;
using Microsoft.Extensions.Options;

namespace GitVersion
{
    public class GitRepository : IGitRepository
    {
        public IGitRepositoryCommands Commands { get; }
        private Lazy<IRepository> repositoryLazy;
        private IRepository repositoryInstance => repositoryLazy.Value;

        public GitRepository(IOptions<GitVersionOptions> options)
            : this(() => options.Value.GitRootPath)
        {
        }

        public GitRepository(IRepository repository)
        {
            repositoryLazy = new Lazy<IRepository>(() => repository);
            Commands = new GitRepositoryCommands(repositoryLazy);
        }

        public GitRepository(Func<string> getGitRootDirectory)
        {
            repositoryLazy = new Lazy<IRepository>(() => new Repository(getGitRootDirectory()));
            Commands = new GitRepositoryCommands(repositoryLazy);
        }

        public void Dispose()
        {
            if (repositoryLazy.IsValueCreated) repositoryInstance.Dispose();
        }

        public string Path => repositoryInstance.Info.Path;
        public bool IsHeadDetached => repositoryInstance.Info.IsHeadDetached;
        public int GetNumberOfUncommittedChanges()
        {
            // check if we have a branch tip at all to behave properly with empty repos
            // => return that we have actually uncomitted changes because we are apparently
            // running GitVersion on something which lives inside this brand new repo _/\Ö/\_
            if (repositoryInstance.Head?.Tip == null || repositoryInstance.Diff == null)
            {
                // this is a somewhat cumbersome way of figuring out the number of changes in the repo
                // which is more expensive than to use the Diff as it gathers more info, but
                // we can't use the other method when we are dealing with a new/empty repo
                try
                {
                    var status = repositoryInstance.RetrieveStatus();
                    return status.Untracked.Count() + status.Staged.Count();

                }
                catch (Exception)
                {
                    return Int32.MaxValue; // this should be somewhat puzzling to see,
                    // so we may have reached our goal to show that
                    // that repo is really "Dirty"...
                }
            }

            // gets all changes of the last commit vs Staging area and WT
            var changes = repositoryInstance.Diff.Compare<TreeChanges>(repositoryInstance.Head.Tip.Tree,
                DiffTargets.Index | DiffTargets.WorkingDirectory);

            return changes.Count;
        }
        public Commit FindMergeBase(Commit commit, Commit otherCommit)
        {
            return (Commit)repositoryInstance.ObjectDatabase.FindMergeBase(commit, otherCommit);
        }
        public string ShortenObjectId(Commit commit)
        {
            return repositoryInstance.ObjectDatabase.ShortenObjectId(commit);
        }

        public Branch Head => (Branch)repositoryInstance.Head;

        public ReferenceCollection Refs => (ReferenceCollection)repositoryInstance.Refs;

        public CommitCollection Commits => CommitCollection.FromCommitLog(repositoryInstance.Commits);

        public BranchCollection Branches => (BranchCollection)repositoryInstance.Branches;

        public TagCollection Tags => (TagCollection)repositoryInstance.Tags;

        public Network Network => repositoryInstance.Network;
    }
}
