using System;
using GitVersion.Helpers;
using System.Threading.Tasks;

public class MockThreadSleep : IThreadSleep
{
    private Func<int, Task> Validator;

    public MockThreadSleep(Func<int, Task> validator = null)
    {
        this.Validator = validator;
    }

    public async Task SleepAsync(int milliseconds)
    {
        if (Validator != null)
        {
            await Validator(milliseconds);
        }
    }
}
