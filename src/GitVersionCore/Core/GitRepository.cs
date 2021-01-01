using System;
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

        public RepositoryStatus RetrieveStatus() => repositoryInstance.RetrieveStatus();

        public Branch Head => (Branch)repositoryInstance.Head;

        public ReferenceCollection Refs => (ReferenceCollection)repositoryInstance.Refs;

        public CommitCollection Commits => CommitCollection.FromCommitLog(repositoryInstance.Commits);

        public BranchCollection Branches => (BranchCollection)repositoryInstance.Branches;

        public TagCollection Tags => (TagCollection)repositoryInstance.Tags;

        public RepositoryInformation Info => repositoryInstance.Info;

        public Diff Diff => repositoryInstance.Diff;

        public ObjectDatabase ObjectDatabase => repositoryInstance.ObjectDatabase;

        public Network Network => repositoryInstance.Network;
    }
}
