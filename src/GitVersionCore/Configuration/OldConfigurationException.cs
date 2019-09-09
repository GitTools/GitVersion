using System;
using GitVersion.Exceptions;

namespace GitVersion.Configuration
{
    [Serializable]
    public class OldConfigurationException : GitVersionException
    {
        public OldConfigurationException(string message)
            : base(message)
        {
        }
    }
}