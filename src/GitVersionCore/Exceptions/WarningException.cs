namespace GitVersion
{
    using System;

    [Serializable]
    public class WarningException : Exception
    {
        public WarningException(string message)
            : base(message)
        {
        }
    }
}