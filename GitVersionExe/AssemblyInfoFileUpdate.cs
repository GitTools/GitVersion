namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    class AssemblyInfoFileUpdate : IDisposable
    {
        List<Action> restoreBackupTasks = new List<Action>();
        List<Action> cleanupBackupTasks = new List<Action>();

        public AssemblyInfoFileUpdate(Arguments args, string workingDirectory, Dictionary<string, string> variables, IFileSystem fileSystem)
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

                string assemblyVersion;
                if (!string.IsNullOrWhiteSpace(args.AssemblyVersionFormat))
                {
                    assemblyVersion = variables[args.AssemblyVersionFormat];
                }
                else
                {
                    assemblyVersion = string.Format("{0}.{1}.0.0", variables[VariableProvider.Major], variables[VariableProvider.Minor]);
                }

                var assemblyInfoVersion = variables[VariableProvider.InformationalVersion];
                var assemblyFileVersion = variables[VariableProvider.AssemblySemVer];
                var fileContents = fileSystem.ReadAllText(assemblyInfoFile)
                    .RegexReplace(@"AssemblyVersion\(""\d+.\d+.\d+(.\d+|\*)?""\)", string.Format("AssemblyVersion(\"{0}\")", assemblyVersion))
                    .RegexReplace(@"AssemblyInformationalVersion\(""\d+.\d+.\d+(.\d+|\*)?""\)", string.Format("AssemblyInformationalVersion(\"{0}\")", assemblyInfoVersion))
                    .RegexReplace(@"AssemblyFileVersion\(""\d+.\d+.\d+(.\d+|\*)?""\)", string.Format("AssemblyFileVersion(\"{0}\")", assemblyFileVersion));

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

            return fileSystem.DirectoryGetFiles(workingDirectory, "AssemblyInfo.cs", SearchOption.AllDirectories);
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