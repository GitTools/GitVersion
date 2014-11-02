namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    class AssemblyInfoFileUpdate : IDisposable
    {
        List<Action> restoreBackupTasks = new List<Action>();
        List<Action> cleanupBackupTasks = new List<Action>();

        public AssemblyInfoFileUpdate(Arguments args, string workingDirectory, Dictionary<string, string> variables)
        {
            if (!args.UpdateAssemblyInfo) return;

            if (args.Output != OutputType.Json)
                Console.WriteLine("Updating assembly info files");

            var assemblyInfoFiles = GetAssemblyInfoFiles(workingDirectory, args);

            foreach (var assemblyInfoFile in assemblyInfoFiles)
            {
                var backupAssemblyInfo = assemblyInfoFile + ".bak";
                var localAssemblyInfo = assemblyInfoFile;
                File.Copy(assemblyInfoFile, backupAssemblyInfo, true);
                restoreBackupTasks.Add(() =>
                {
                    if (File.Exists(localAssemblyInfo))
                        File.Delete(localAssemblyInfo);
                    File.Move(backupAssemblyInfo, localAssemblyInfo);
                });
                cleanupBackupTasks.Add(() => File.Delete(backupAssemblyInfo));

                var assemblyVersion = string.Format("{0}.{1}.0.0", variables[VariableProvider.Major], variables[VariableProvider.Minor]);
                var assemblyInfoVersion = variables[VariableProvider.InformationalVersion];
                var assemblyFileVersion = variables[VariableProvider.AssemblySemVer];
                var fileContents = File.ReadAllText(assemblyInfoFile)
                    .RegexReplace(@"AssemblyVersion\(""\d+.\d+.\d+(.\d+|\*)?""\)", string.Format("AssemblyVersion(\"{0}\")", assemblyVersion))
                    .RegexReplace(@"AssemblyInformationalVersion\(""\d+.\d+.\d+(.\d+|\*)?""\)", string.Format("AssemblyInformationalVersion(\"{0}\")", assemblyInfoVersion))
                    .RegexReplace(@"AssemblyFileVersion\(""\d+.\d+.\d+(.\d+|\*)?""\)", string.Format("AssemblyFileVersion(\"{0}\")", assemblyFileVersion));

                File.WriteAllText(assemblyInfoFile, fileContents);
            }
        }

        static IEnumerable<string> GetAssemblyInfoFiles(string workingDirectory, Arguments args)
        {
            if (args.UpdateAssemblyInfoFileName != null)
            {
                var fullPath = Path.Combine(workingDirectory, args.UpdateAssemblyInfoFileName);

                if (File.Exists(fullPath))
                {
                    return new[] { fullPath };
                }
            }

            return Directory.GetFiles(workingDirectory, "AssemblyInfo.cs", SearchOption.AllDirectories);
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