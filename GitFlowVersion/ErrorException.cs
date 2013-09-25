namespace GitFlowVersion
{
    using System;

    public class ErrorException : Exception
    {
        public ErrorException(string message)
            : base(message)
        {

        }
    }
}