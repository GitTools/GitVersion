namespace GitTools.IO
{
    using System;
    using System.IO;

    public static class DeleteHelper
    {
        public static void DeleteGitRepository(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            try
            {
                foreach (var fileName in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
                {
                    var fileInfo = new FileInfo(fileName)
                    {
                        IsReadOnly = false
                    };

                    try
                    {
                        fileInfo.Delete();
                    }
                    catch (FileNotFoundException)
                    {
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }

                Directory.Delete(directory, true);
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }


        public static void DeleteDirectory(string directory, bool recursive)
        {
            try
            {
                Directory.Delete(directory, recursive);
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}