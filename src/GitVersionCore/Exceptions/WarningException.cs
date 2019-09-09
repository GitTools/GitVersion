using System;

namespace GitVersion.Exceptions
{
    [Serializable]
    public class WarningException : Exception
    {
        public WarningException(string message)
            : base(message)
        {
        }
    }
}