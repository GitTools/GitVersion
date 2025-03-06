namespace GitVersion.Helpers;

internal static class DirectoryHelper
{
    public static void DeleteDirectory(string directoryPath)
    {
        // From http://stackoverflow.com/questions/329355/cannot-delete-directory-with-directory-deletepath-true/329502#329502

        if (!Directory.Exists(directoryPath))
        {
            Trace.WriteLine(
                $"Directory '{directoryPath}' is missing and can't be removed.");

            return;
        }

        var files = Directory.GetFiles(directoryPath);
        var dirs = Directory.GetDirectories(directoryPath);

        foreach (var file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (var dir in dirs)
        {
            DeleteDirectory(dir);
        }

        File.SetAttributes(directoryPath, FileAttributes.Normal);
        try
        {
            Directory.Delete(directoryPath, false);
        }
        catch (IOException)
        {
            Trace.WriteLine(string.Format("{0}The directory '{1}' could not be deleted!" +
                                          "{0}Most of the time, this is due to an external process accessing the files in the temporary repositories created during the test runs, and keeping a handle on the directory, thus preventing the deletion of those files." +
                                          "{0}Known and common causes include:" +
                                          "{0}- Windows Search Indexer (go to the Indexing Options, in the Windows Control Panel, and exclude the bin folder of LibGit2Sharp.Tests)" +
                                          "{0}- Antivirus (exclude the bin folder of LibGit2Sharp.Tests from the paths scanned by your real-time antivirus){0}",
                PathHelper.NewLine, PathHelper.GetFullPath(directoryPath)));
        }
    }
}
