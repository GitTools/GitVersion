using System;
using System.IO;

namespace GitTools.Testing.Internal
{
    static class PathHelper
    {
        public static string GetTempPath() => Path.Combine(Path.GetTempPath(), "TestRepositories", Guid.NewGuid().ToString());
    }
}
