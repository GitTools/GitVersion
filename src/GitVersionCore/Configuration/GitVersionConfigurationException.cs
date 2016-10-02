namespace GitVersion
{
    using System;

    public class GitVersionConfigurationException : Exception
    {
        public GitVersionConfigurationException(string msg) : base(msg)
        {
        }
    }
}