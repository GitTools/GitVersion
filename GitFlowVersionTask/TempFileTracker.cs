namespace GitFlowVersionTask
{
    using System;
    using System.IO;

    public static class TempFileTracker
    {
        public static string tempPath;
        static TempFileTracker()
        {
            tempPath = Path.Combine(Path.GetTempPath(), "GitFlowVersionTask");
            Directory.CreateDirectory(tempPath);
        }

        public static void DeleteTempFiles()
        {
            foreach (var file in Directory.GetFiles(tempPath))
            {
                if (File.GetLastWriteTime(file) < DateTime.Now.AddHours(-1))
                {
                    File.Delete(file);
                }
            }
        }

        
    }
}