using System;

namespace GitVersion.Configuration
{
    [Serializable]
    public class GitVersionConfigurationException : GitVersionException
    {
        public GitVersionConfigurationException(string msg)
            : base(msg)
        {
        }
    }
}
