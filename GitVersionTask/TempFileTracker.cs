using System;
using System.IO;

public static class TempFileTracker
{
    public static string TempPath;

    static TempFileTracker()
    {
        TempPath = Path.Combine(Path.GetTempPath(), "GitVersionTask");
        Directory.CreateDirectory(TempPath);
    }

    public static void DeleteTempFiles()
    {
        if (!Directory.Exists(TempPath))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(TempPath))
        {
            if (File.GetLastWriteTime(file) < DateTime.Now.AddDays(-1))
            {
                try
                {
                    File.Delete(file);
                }
                catch (UnauthorizedAccessException)
                {
                    //ignore contention
                }
            }
        }
    }

}