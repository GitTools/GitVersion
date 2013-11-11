namespace GitFlowVersion
{
    using System;
    using LibGit2Sharp;

    public class MissingBranchException : Exception
    {
        public MissingBranchException(string message, LibGit2SharpException exception):base(message,exception)
        {
        }
    }
}