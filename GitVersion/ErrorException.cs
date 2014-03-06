namespace GitVersion
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class ErrorException : Exception
    {
        public ErrorException(string message)
            : base(message)
        {
        }

        protected ErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}