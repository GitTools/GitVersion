using System.Threading.Tasks;

namespace GitVersion
{
    public class ThreadSleep : IThreadSleep
    {
        public async Task SleepAsync(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }
    }
}
