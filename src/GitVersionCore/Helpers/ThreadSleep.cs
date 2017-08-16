namespace GitVersion.Helpers
{
    using System.Threading.Tasks;

    internal class ThreadSleep : IThreadSleep
    {
        public async Task SleepAsync(int milliseconds)
        {
            await TaskHelper.Delay(milliseconds);
        }
    }
}
