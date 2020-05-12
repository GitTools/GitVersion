using System;
using System.Collections.Generic;
using LibGit2Sharp;

namespace GitVersion
{
    /// <summary>
    /// Default implementation of <see cref="IGitRepositoryCommands"/> using
    /// the <c>static</c> <see cref="Commands"/> <c>class</c>.
    /// </summary>
    public class GitRepositoryCommands : IGitRepositoryCommands
    {
        private readonly Lazy<IRepository> lazyRepository;
        private IRepository repository => lazyRepository.Value;

        public GitRepositoryCommands(Lazy<IRepository> lazyRepository)
        {
            this.lazyRepository = lazyRepository ?? throw new ArgumentNullException(nameof(lazyRepository));
        }

        public Branch Checkout(string committishOrBranchSpec)
        {
            return Commands.Checkout(this.repository, committishOrBranchSpec);
        }

        public Branch Checkout(string committishOrBranchSpec, CheckoutOptions options)
        {
            return Commands.Checkout(this.repository, committishOrBranchSpec, options);
        }

        public Branch Checkout(Branch branch)
        {
            return Commands.Checkout(this.repository, branch);
        }

        public Branch Checkout(Branch branch, CheckoutOptions options)
        {
            return Commands.Checkout(this.repository, branch, options);
        }

        public Branch Checkout(Commit commit)
        {
            return Commands.Checkout(this.repository, commit);
        }

        public Branch Checkout(Commit commit, CheckoutOptions options)
        {
            return Commands.Checkout(this.repository, commit, options);
        }

        public void Checkout(Tree tree, CheckoutOptions checkoutOptions, string refLogHeadSpec)
        {
            Commands.Checkout(this.repository, tree, checkoutOptions, refLogHeadSpec);
        }

        public void Fetch(string remote, IEnumerable<string> refspecs, FetchOptions options, string logMessage)
        {
            Commands.Fetch((Repository)this.repository, remote, refspecs, options, logMessage);
        }

        public void Move(string sourcePath, string destinationPath)
        {
            Commands.Move(this.repository, sourcePath, destinationPath);
        }

        public void Move(IEnumerable<string> sourcePaths, IEnumerable<string> destinationPaths)
        {
            Commands.Move(this.repository, sourcePaths, destinationPaths);
        }

        public MergeResult Pull(Signature merger, PullOptions options)
        {
            return Commands.Pull((Repository)this.repository, merger, options);
        }

        public void Remove(string path, bool removeFromWorkingDirectory)
        {
            Commands.Remove(this.repository, path, removeFromWorkingDirectory);
        }

        public void Remove(IEnumerable<string> paths)
        {
            Commands.Remove(this.repository, paths);
        }

        public void Remove(IEnumerable<string> paths, bool removeFromWorkingDirectory, ExplicitPathsOptions explicitPathsOptions)
        {
            Commands.Remove(this.repository, paths, removeFromWorkingDirectory, explicitPathsOptions);
        }

        public void Remove(string path)
        {
            Commands.Remove(this.repository, path);
        }

        public void Remove(string path, bool removeFromWorkingDirectory, ExplicitPathsOptions explicitPathsOptions)
        {
            Commands.Remove(this.repository, path, removeFromWorkingDirectory, explicitPathsOptions);
        }

        public void Stage(string path)
        {
            Commands.Stage(this.repository, path);
        }

        public void Stage(string path, StageOptions stageOptions)
        {
            Commands.Stage(this.repository, path, stageOptions);
        }

        public void Stage(IEnumerable<string> paths)
        {
            Commands.Stage(this.repository, paths);
        }

        public void Stage(IEnumerable<string> paths, StageOptions stageOptions)
        {
            Commands.Stage(this.repository, paths, stageOptions);
        }

        public void Unstage(string path)
        {
            Commands.Unstage(this.repository, path);
        }

        public void Unstage(string path, ExplicitPathsOptions explicitPathsOptions)
        {
            Commands.Unstage(this.repository, path, explicitPathsOptions);
        }

        public void Unstage(IEnumerable<string> paths)
        {
            Commands.Unstage(this.repository, paths);
        }

        public void Unstage(IEnumerable<string> paths, ExplicitPathsOptions explicitPathsOptions)
        {
            Commands.Unstage(this.repository, paths, explicitPathsOptions);
        }
    }
}
