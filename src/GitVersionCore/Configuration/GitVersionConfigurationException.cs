namespace GitVersion
{
    using System;

    [Serializable]
    public class GitVersionConfigurationException : GitVersionException
    {
        public GitVersionConfigurationException(string msg)
            : base(msg)
        {
        }
    }
}