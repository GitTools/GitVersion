using System;

namespace GitVersion.Model.Exceptions
{
    public class InfiniteLoopProtectionException : Exception
    {
        public InfiniteLoopProtectionException(string messageFormat)
            : base(messageFormat)
        {
        }
    }
}
