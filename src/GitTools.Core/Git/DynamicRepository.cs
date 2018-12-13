namespace GitTools.Git
{
    using System;
    using LibGit2Sharp;

    public class DynamicRepository : IDisposable
    {
        readonly Action _dispose;

        public DynamicRepository(Repository repository, Action dispose)
        {
            Repository = repository;
            _dispose = dispose;
        }

        public Repository Repository { get; private set; }

        public void Dispose()
        {
            try
            {
                Repository.Dispose();

            }
            finally
            {
                _dispose();
            }
        }
    }
}