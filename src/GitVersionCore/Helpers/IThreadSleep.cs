using System.Threading.Tasks;

namespace GitVersion.Helpers
{
    public interface IThreadSleep
    {
        Task SleepAsync(int milliseconds);
    }
}
