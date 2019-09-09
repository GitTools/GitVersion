using System;
using System.Threading.Tasks;
using GitVersion.Common;

namespace GitVersionCore.Tests.Mocks
{
    public class MockThreadSleep : IThreadSleep
    {
        private Func<int, Task> Validator;

        public MockThreadSleep(Func<int, Task> validator = null)
        {
            Validator = validator;
        }

        public async Task SleepAsync(int milliseconds)
        {
            if (Validator != null)
            {
                await Validator(milliseconds);
            }
        }
    }
}
