using System;
using GitVersion.Helpers;

public class MockThreadSleep : IThreadSleep
{
    private Action<int> Validator;
     
    public MockThreadSleep(Action<int> validator = null)
    {
        this.Validator = validator;
    }

    public void Sleep(int milliseconds)
    {
        if (Validator != null)
        {
            Validator(milliseconds);
        }
    }
}
