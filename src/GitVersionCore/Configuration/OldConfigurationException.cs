namespace GitVersion
{
    using System;

    [Serializable]
    public class OldConfigurationException : GitVersionException
    {
        public OldConfigurationException(string message)
            : base(message)
        {
        }
    }
}