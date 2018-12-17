namespace GitTools.Testing
{
    using System;

    /// <summary>
    /// VirtualTime starts at an hour before now, then each time it is called increments by a minute
    /// Useful when interacting with git to make sure commits and other interactions are not at the same time
    /// </summary>
    public static class VirtualTime
    {
        static DateTimeOffset _simulatedTime = DateTimeOffset.Now.AddHours(-1);

        /// <summary>
        /// Increments by 1 minute each time it is called
        /// </summary>
        public static DateTimeOffset Now
        {
            get
            {
                _simulatedTime = _simulatedTime.AddMinutes(1);
                return _simulatedTime;
            }
        }
    }
}