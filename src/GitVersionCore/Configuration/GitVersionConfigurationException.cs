using System;
using GitVersion.Exceptions;

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