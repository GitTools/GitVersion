namespace GitVersion.Helpers
{
    using System.Threading;

    internal class ThreadSleep : IThreadSleep
    {
        public void Sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }
    }
}
