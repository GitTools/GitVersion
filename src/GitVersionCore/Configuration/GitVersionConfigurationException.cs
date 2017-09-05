namespace GitVersion
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class GitVersionConfigurationException : GitVersionException
    {
        public GitVersionConfigurationException(string msg)
            : base(msg)
        {
        }


        protected GitVersionConfigurationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}