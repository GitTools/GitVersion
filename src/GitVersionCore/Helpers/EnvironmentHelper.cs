using System;

namespace GitVersion.Helpers
{
    public class EnvironmentHelper
    {
        public static string GetEnvironmentVariableForProcess(string envVar)
        {
            return System.Environment.GetEnvironmentVariable(envVar, EnvironmentVariableTarget.Process);
        }
    }
}
