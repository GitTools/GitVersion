using System;

namespace GitVersion.Helpers
{
    public class EnvironmentHelper
    {
        public static string GetEnvironmentVariableForProcess(string envVar)
        {
            return Environment.GetEnvironmentVariable(envVar, EnvironmentVariableTarget.Process);
        }
    }
}
