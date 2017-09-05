namespace GitVersion
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class GitVersionException : ApplicationException
    {
        public GitVersionException()
        {
        }


        public GitVersionException(string message)
            : base(message)
        {
        }


        public GitVersionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }


        protected GitVersionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}