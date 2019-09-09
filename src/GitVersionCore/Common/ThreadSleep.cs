using System.Threading.Tasks;

namespace GitVersion.Common
{
    internal class ThreadSleep : IThreadSleep
    {
        public async Task SleepAsync(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }
    }
}
