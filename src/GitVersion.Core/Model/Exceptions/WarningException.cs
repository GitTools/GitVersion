using System;

namespace GitVersion
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
