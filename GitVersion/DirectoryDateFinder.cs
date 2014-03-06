namespace GitVersion
{
    using System;
    using System.IO;

    static class DirectoryDateFinder
    {
        public static long GetLastDirectoryWrite(string path)
        {
            var lastHigh = DateTime.MinValue;
            foreach (var file in Directory.EnumerateDirectories(path, "*.*", SearchOption.AllDirectories))
            {
                var lastWriteTime = File.GetLastWriteTime(file);
                if (lastWriteTime > lastHigh)
                {
                    lastHigh = lastWriteTime;
                }
            }
            return lastHigh.Ticks;
        }
    }
}