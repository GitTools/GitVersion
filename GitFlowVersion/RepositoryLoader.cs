namespace GitFlowVersion
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
                    throw new ErrorException("Restart of Visual Studio required due to update of 'GitFlowVersion.Fody'.");
                }
                throw;
            }
        }
    }
}