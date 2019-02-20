namespace GitVersion
{
    using System;

    public class BugException : Exception
    {
        public BugException(string message) : base(message)
        {
        }
    }
}