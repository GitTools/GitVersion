using System.IO;

namespace GitVersion.Helpers
{
    public static class DeleteHelper
    {
        public static void DeleteGitRepository(string directory)
        {
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            foreach (var fileName in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                var fileInfo = new FileInfo(fileName)
                {
                    IsReadOnly = false
                };

                fileInfo.Delete();
            }

            Directory.Delete(directory, true);
        }
    }
}
