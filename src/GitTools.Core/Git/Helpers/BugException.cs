namespace GitTools.Git
{
    using System;

    public class BugException : Exception
    {
        public BugException(string message) : base(message)
        {
        }
    }
}