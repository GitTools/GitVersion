namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using GitVersion.Helpers;

    class AssemblyInfoFileUpdate : IDisposable
    {
        List<Action> restoreBackupTasks = new List<Action>();
        List<Action> cleanupBackupTasks = new List<Action>();

        public AssemblyInfoFileUpdate(Arguments args, string workingDirectory, VersionVariables variables, IFileSystem fileSystem)
        {
            if (!args.UpdateAssemblyInfo) return;

            if (args.Output != OutputType.Json)
                Console.WriteLine("Updating assembly info files");

            var assemblyInfoFiles = GetAssemblyInfoFiles(workingDirectory, args, fileSystem);

            foreach (var assemblyInfoFile in assemblyInfoFiles)
            {
                var backupAssemblyInfo = assemblyInfoFile + ".bak";
                var localAssemblyInfo = assemblyInfoFile;
                fileSystem.Copy(assemblyInfoFile, backupAssemblyInfo, true);
                restoreBackupTasks.Add(() =>
                {
                    if (fileSystem.Exists(localAssemblyInfo))
                        fileSystem.Delete(localAssemblyInfo);
                    fileSystem.Move(backupAssemblyInfo, localAssemblyInfo);
                });
                cleanupBackupTasks.Add(() => fileSystem.Delete(backupAssemblyInfo));

                var assemblyVersion = variables.AssemblySemVer;
                var assemblyInfoVersion = variables.InformationalVersion;
                var assemblyFileVersion = variables.MajorMinorPatch + ".0";
                var fileContents = fileSystem.ReadAllText(assemblyInfoFile)
                    .RegexReplace(@"AssemblyVersion\(""[^""]*""\)", string.Format("AssemblyVersion(\"{0}\")", assemblyVersion))
                    .RegexReplace(@"AssemblyInformationalVersion\(""[^""]*""\)", string.Format("AssemblyInformationalVersion(\"{0}\")", assemblyInfoVersion))
                    .RegexReplace(@"AssemblyFileVersion\(""[^""]*""\)", string.Format("AssemblyFileVersion(\"{0}\")", assemblyFileVersion));

                fileSystem.WriteAllText(assemblyInfoFile, fileContents);
            }
        }

        static IEnumerable<string> GetAssemblyInfoFiles(string workingDirectory, Arguments args, IFileSystem fileSystem)
        {
            if (args.UpdateAssemblyInfoFileName != null)
            {
                var fullPath = Path.Combine(workingDirectory, args.UpdateAssemblyInfoFileName);

                if (fileSystem.Exists(fullPath))
                {
                    return new[] { fullPath };
                }
            }

            return fileSystem.DirectoryGetFiles(workingDirectory, "AssemblyInfo.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".vb", StringComparison.OrdinalIgnoreCase));
        }

        public void Dispose()
        {
            foreach (var restoreBackup in restoreBackupTasks)
            {
                restoreBackup();
            }

            cleanupBackupTasks.Clear();
            restoreBackupTasks.Clear();
        }

        public void DoNotRestoreAssemblyInfo()
        {
            foreach (var cleanupBackupTask in cleanupBackupTasks)
            {
                cleanupBackupTask();
            }
            cleanupBackupTasks.Clear();
            restoreBackupTasks.Clear();
        }
    }
}