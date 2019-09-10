using System.Threading.Tasks;

namespace GitVersion.Common
{
    public interface IThreadSleep
    {
        Task SleepAsync(int milliseconds);
    }
}
