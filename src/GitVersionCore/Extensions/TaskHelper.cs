namespace System.Threading.Tasks
{
    public static class TaskHelper
    {

#if NETDESKTOP
        public static Task Delay(int milliseconds)
        {
            var tcs = new TaskCompletionSource<bool>();
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += (obj, args) =>
            {
                tcs.TrySetResult(true);
            };
            timer.Interval = (double)milliseconds;
            timer.AutoReset = false;
            timer.Start();
            return tcs.Task;
        }
#else
        public static Task Delay(int milliseconds)
        {
            return Task.Delay(milliseconds);
        }
#endif
    }


}
