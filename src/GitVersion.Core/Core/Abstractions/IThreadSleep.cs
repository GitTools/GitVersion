using System.Threading.Tasks;

namespace GitVersion
{
    public interface IThreadSleep
    {
        Task SleepAsync(int milliseconds);
    }
}
