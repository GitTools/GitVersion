 // ReSharper disable once CheckNamespace

namespace GitTools.Testing
{
    using System;
    using Internal;
    using LibGit2Sharp;
    using Logging;

    /// <summary>
    ///     Fixture abstracting a git repository
    /// </summary>
    public abstract class RepositoryFixtureBase : IDisposable
    {
        static readonly ILog Logger = LogProvider.For<RepositoryFixtureBase>();
        readonly SequenceDiagram _sequenceDiagram;

        protected RepositoryFixtureBase(Func<string, IRepository> repoBuilder)
            : this(repoBuilder(PathHelper.GetTempPath()))
        {
        }

        protected RepositoryFixtureBase(IRepository repository)
        {
            _sequenceDiagram = new SequenceDiagram();
            Repository = repository;
            Repository.Config.Set("user.name", "Test");
            Repository.Config.Set("user.email", "test@email.com");
        }

        public IRepository Repository { get; private set; }

        public string RepositoryPath
        {
            get { return Repository.Info.WorkingDirectory.TrimEnd('\\'); }
        }

        public SequenceDiagram SequenceDiagram
        {
            get { return _sequenceDiagram; }
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            Repository.Dispose();

            try
            {
                DirectoryHelper.DeleteDirectory(RepositoryPath);
            }
            catch (Exception e)
            {
                Logger.InfoFormat("Failed to clean up repository path at {0}. Received exception: {1}", RepositoryPath,
                    e.Message);
            }

            _sequenceDiagram.End();
            Logger.InfoFormat("**Visualisation of test:**");
            Logger.InfoFormat(string.Empty);
            Logger.InfoFormat(_sequenceDiagram.GetDiagram());
        }

        public void Checkout(string branch)
        {
            Commands.Checkout(Repository, branch);
        }

        public void MakeATaggedCommit(string tag)
        {
            MakeACommit();
            ApplyTag(tag);
        }

        public void ApplyTag(string tag)
        {
            _sequenceDiagram.ApplyTag(tag, Repository.Head.FriendlyName);
            Repository.ApplyTag(tag);
        }

        public void BranchTo(string branchName, string @as = null)
        {
            _sequenceDiagram.BranchTo(branchName, Repository.Head.FriendlyName, @as);
            var branch = Repository.CreateBranch(branchName);
            Commands.Checkout(Repository, branch);
        }

        public void BranchToFromTag(string branchName, string fromTag, string onBranch, string @as = null)
        {
            _sequenceDiagram.BranchToFromTag(branchName, fromTag, onBranch, @as);
            var branch = Repository.CreateBranch(branchName);
            Commands.Checkout(Repository, branch);
        }

        public void MakeACommit()
        {
            var to = Repository.Head.FriendlyName;
            _sequenceDiagram.MakeACommit(to);
            Repository.MakeACommit();
        }

        /// <summary>
        ///     Merges (no-ff) specified branch into the current HEAD of this repository
        /// </summary>
        public void MergeNoFF(string mergeSource)
        {
            _sequenceDiagram.Merge(mergeSource, Repository.Head.FriendlyName);
            Repository.MergeNoFF(mergeSource, Generate.SignatureNow());
        }

        /// <summary>
        ///     Clones the repository managed by this fixture into another LocalRepositoryFixture
        /// </summary>
        public LocalRepositoryFixture CloneRepository()
        {
            var localPath = PathHelper.GetTempPath();
            LibGit2Sharp.Repository.Clone(RepositoryPath, localPath);
            return new LocalRepositoryFixture(new Repository(localPath));
        }
    }
}