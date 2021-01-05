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

        public void Checkout(string committishOrBranchSpec)
        {
            Commands.Checkout(repository, committishOrBranchSpec);
        }

        public void Checkout(Branch branch)
        {
            Commands.Checkout(repository, branch);
        }

        public void Fetch(string remote, IEnumerable<string> refspecs, AuthenticationInfo auth, string logMessage)
        {
            Commands.Fetch((Repository)repository, remote, refspecs, GitRepository.GetFetchOptions(auth), logMessage);
        }
    }
}
