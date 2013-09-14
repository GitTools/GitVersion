using System;
using LibGit2Sharp;

public class RepositoryLoader
{
    public static Repository GetRepo(string gitDir)
    {
        try
        {
            return new Repository(gitDir);
        }
        catch (Exception exception)
        {
            if (exception.Message.Contains("LibGit2Sharp.Core.NativeMethods") || exception.Message.Contains("FilePathMarshaler"))
            {
                throw new WeavingException("Restart of Visual Studio required due to update of 'GitFlowVersion.Fody'.");
            }
            throw;
        }
    }
}