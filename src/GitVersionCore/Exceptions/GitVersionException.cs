namespace GitVersion
{
    using System;

    [Serializable]
    public class GitVersionException : GitToolsException
    {
        public GitVersionException(string message)
            : base(message)
        {
        }


        public GitVersionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
