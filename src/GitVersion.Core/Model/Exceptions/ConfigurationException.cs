using System;

namespace GitVersion.Configuration
{
    [Serializable]
    public class ConfigurationException : GitVersionException
    {
        public ConfigurationException(string msg)
            : base(msg)
        {
        }
    }
}
