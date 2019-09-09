using System.Threading.Tasks;

namespace GitVersion.Helpers
{
    internal class ThreadSleep : IThreadSleep
    {
        public async Task SleepAsync(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }
    }
}
