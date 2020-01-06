using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GitVersionCore.Tests.Helpers
{
    public static class DirectoryHelper
    {
        private static readonly Dictionary<string, string> ToRename = new Dictionary<string, string>
        {
            {"gitted", ".git"},
            {"gitmodules", ".gitmodules"},
        };

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            // From http://stackoverflow.com/questions/58744/best-way-to-copy-the-entire-contents-of-a-directory-in-c/58779#58779

            foreach (var dir in source.GetDirectories())
            {
                CopyFilesRecursively(dir, target.CreateSubdirectory(Rename(dir.Name)));
            }
            foreach (var file in source.GetFiles())
            {
                file.CopyTo(Path.Combine(target.FullName, Rename(file.Name)));
            }
        }

        private static string Rename(string name)
        {
            return ToRename.ContainsKey(name) ? ToRename[name] : name;
        }

        public static void DeleteSubDirectories(string parentPath)
        {
            var dirs = Directory.GetDirectories(parentPath);
            foreach (var dir in dirs)
            {
                DeleteDirectory(dir);
            }
        }

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
                    Environment.NewLine, Path.GetFullPath(directoryPath)));
            }
        }
    }
}