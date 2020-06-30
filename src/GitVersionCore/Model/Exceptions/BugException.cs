using System;

namespace GitVersion
{
    public class BugException : Exception
    {
        public BugException(string message) : base(message)
        {
        }
    }
}
