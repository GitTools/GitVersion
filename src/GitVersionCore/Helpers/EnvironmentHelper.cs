using System;

namespace GitVersion.Helpers
{
    public class EnvironmentHelper
    {
        public static string GetEnvironmentVariableForProcess(string envVar)
        {
#if NETDESKTOP
            return Environment.GetEnvironmentVariable(envVar, EnvironmentVariableTarget.Process);
#else
            return Environment.GetEnvironmentVariable(envVar);
#endif
        }
    }
}
