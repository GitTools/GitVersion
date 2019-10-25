using System;
using System.Threading.Tasks;
using GitVersion;

namespace GitVersionCore.Tests.Mocks
{
    public class MockThreadSleep : IThreadSleep
    {
        private readonly Func<int, Task> validator;

        public MockThreadSleep(Func<int, Task> validator = null)
        {
            this.validator = validator;
        }

        public async Task SleepAsync(int milliseconds)
        {
            if (validator != null)
            {
                await validator(milliseconds);
            }
        }
    }
}
