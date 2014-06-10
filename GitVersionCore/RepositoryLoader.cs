namespace GitVersion
{
    using System;
    using LibGit2Sharp;

    public class RepositoryLoader
    {
        public static Repository GetRepo(string gitDirectory)
        {
            try
            {
                return new Repository(gitDirectory);
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains("LibGit2Sharp.Core.NativeMethods") || exception.Message.Contains("FilePathMarshaler"))
                {
                    throw new WarningException("Restart of the process may be required to load an updated version of LibGit2Sharp.");
                }
                throw;
            }
        }
    }
}