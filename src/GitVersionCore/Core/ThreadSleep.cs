using System.Threading.Tasks;

namespace GitVersion
{
    internal class ThreadSleep : IThreadSleep
    {
        public async Task SleepAsync(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }
    }
}
