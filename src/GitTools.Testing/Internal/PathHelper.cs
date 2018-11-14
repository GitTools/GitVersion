namespace GitTools.Testing.Internal
{
    using System;
    using System.IO;

    static class PathHelper
    {
        public static string GetTempPath()
        {
            return Path.Combine(Path.GetTempPath(), "TestRepositories", Guid.NewGuid().ToString());
        }
    }
}