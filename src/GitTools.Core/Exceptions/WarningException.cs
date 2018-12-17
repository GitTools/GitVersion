namespace GitTools
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class WarningException : Exception
    {
        public WarningException(string message)
            : base(message)
        {
        }

#if NETDESKTOP
        protected WarningException(SerializationInfo info, StreamingContext context)
          : base(info, context)
        {
        }
#endif
    }
}