namespace GitTools
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class GitToolsException : Exception
    {
        public GitToolsException(string messageFormat, params object[] args)
            : base(string.Format(messageFormat, args))
        {
        }
        public GitToolsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

#if NETDESKTOP
        protected GitToolsException(SerializationInfo info, StreamingContext context)
          : base(info, context)
        {
        }
#endif
    }
}