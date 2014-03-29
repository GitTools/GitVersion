namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class AssemblyInfoFileUpdate : IDisposable
    {
        private readonly List<Action> _restoreBackupTasks = new List<Action>();
        private readonly List<Action> _cleanupBackupTasks = new List<Action>();

        public AssemblyInfoFileUpdate(Arguments args, string workingDirectory, Dictionary<string, string> variables)
        {
            if (!args.UpdateAssemblyInfo) return;

            if (args.Output != OutputType.Json)
                Console.WriteLine("Updating assembly info files");

            var assemblyInfoFiles = Directory.GetFiles(workingDirectory, "AssemblyInfo.cs",
                SearchOption.AllDirectories);

            foreach (var assemblyInfoFile in assemblyInfoFiles)
            {
                var backupAssemblyInfo = assemblyInfoFile + ".bak";
                var localAssemblyInfo = assemblyInfoFile;
                File.Copy(assemblyInfoFile, backupAssemblyInfo, true);
                _restoreBackupTasks.Add(() =>
                {
                    if (File.Exists(localAssemblyInfo))
                        File.Delete(localAssemblyInfo);
                    File.Move(backupAssemblyInfo, localAssemblyInfo);
                });
                _cleanupBackupTasks.Add(() => File.Delete(backupAssemblyInfo));

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

        public void Dispose()
        {
            foreach (var restoreBackup in _restoreBackupTasks)
            {
                restoreBackup();
            }

            _cleanupBackupTasks.Clear();
            _restoreBackupTasks.Clear();
        }

        public void DoNotRestoreAssemblyInfo()
        {
            foreach (var cleanupBackupTask in _cleanupBackupTasks)
            {
                cleanupBackupTask();
            }
            _cleanupBackupTasks.Clear();
            _restoreBackupTasks.Clear();
        }
    }
}