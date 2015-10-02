namespace GitVersion
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class OldConfigurationException : Exception
    {
        public OldConfigurationException(string message) : base(message)
        {
        }

        protected OldConfigurationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}