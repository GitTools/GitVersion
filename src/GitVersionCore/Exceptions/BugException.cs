using System;

namespace GitVersion.Exceptions
{
    public class BugException : Exception
    {
        public BugException(string message) : base(message)
        {
        }
    }
}