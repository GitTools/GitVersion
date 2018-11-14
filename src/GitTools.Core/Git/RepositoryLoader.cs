namespace GitTools.Git
{
    using System;
    using LibGit2Sharp;
    using Logging;

    public class RepositoryLoader
    {
        static readonly ILog Log = LogProvider.GetLogger(typeof(RepositoryLoader));

        public static Repository GetRepo(string gitDirectory)
        {
            try
            {
                var repository = new Repository(gitDirectory);

                var branch = repository.Head;
                if (branch.Tip == null)
                {
                    throw Log.ErrorAndCreateException<GitToolsException>("No Tip found. Has repo been initialized?");
                }

                return repository;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("LibGit2Sharp.Core.NativeMethods") || ex.Message.Contains("FilePathMarshaler"))
                {
                    throw Log.ErrorAndCreateException<GitToolsException>("Restart of the process may be required to load an updated version of LibGit2Sharp.");
                }

                throw;
            }
        }
    }
}