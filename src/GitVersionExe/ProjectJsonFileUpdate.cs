using GitVersion.Helpers;

namespace GitVersion
{
    using System.IO;
    using System.Linq;

    public class ProjectJsonFileUpdate : FileUpdateBase
    {
        public ProjectJsonFileUpdate(Arguments arguments, string targetPath, VersionVariables variables, IFileSystem fileSystem)
        {
            if (!arguments.UpdateProjectJson)
                return;

            if (arguments.Output != OutputType.Json)
                Logger.WriteInfo("Updating project.json files");

            var files = fileSystem.DirectoryGetFiles(targetPath, "project.json", SearchOption.AllDirectories).ToArray();
            foreach (var file in files)
            {
                ReplaceInFile(variables, fileSystem, file);
            }
        }

        private void ReplaceInFile(VersionVariables variables, IFileSystem fileSystem, string file)
        {
            var backupFile = file + ".bak";
            fileSystem.Copy(file, backupFile, true);
            cleanupBackupTasks.Add(() => fileSystem.Delete(backupFile));
            restoreBackupTasks.Add(() =>
            {
                if (!fileSystem.Exists(backupFile))
                    return;
                fileSystem.Copy(backupFile, file, true);
                fileSystem.Delete(backupFile);
            });

            var json = fileSystem.ReadAllText(file);
            var result = ProjectJsonVersionReplacer.Replace(json, variables);
            if (result.HasError)
            {
                Logger.WriteError(string.Format("An error occured replacing version in {0}: {1}", file, result.Error));
            }
            else if (result.VersionElementNotFound)
            {
                Logger.WriteWarning(string.Format("The version element was not found in {0}", file));
            }
            else
            {
                Logger.WriteInfo(string.Format("Replacing version in {0}", file));
                fileSystem.WriteAllText(file, result.JsonWithReplacement);
            }
        }
    }
}