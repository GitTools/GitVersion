namespace GitVersion
{
    using Helpers;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

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
            Console.WriteLine(string.Format("Found {0} files", assemblyInfoFiles.Count()));

            var assemblyVersion = variables.AssemblySemVer;
            var assemblyVersionRegex = new Regex(@"AssemblyVersion\(""[^""]*""\)");
            var assemblyVersionString = string.Format("AssemblyVersion(\"{0}\")", assemblyVersion);
            var assemblyInfoVersion = variables.InformationalVersion;
            var assemblyInfoVersionRegex = new Regex(@"AssemblyInformationalVersion\(""[^""]*""\)");
            var assemblyInfoVersionString = string.Format("AssemblyInformationalVersion(\"{0}\")", assemblyInfoVersion);
            var assemblyFileVersion = variables.MajorMinorPatch + ".0";
            var assemblyFileVersionRegex = new Regex(@"AssemblyFileVersion\(""[^""]*""\)");
            var assemblyFileVersionString = string.Format("AssemblyFileVersion(\"{0}\")", assemblyFileVersion);

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

                var fileContents = fileSystem.ReadAllText(assemblyInfoFile);
                fileContents = ReplaceOrAppend(assemblyVersionRegex, fileContents, assemblyVersionString);
                fileContents = ReplaceOrAppend(assemblyInfoVersionRegex, fileContents, assemblyInfoVersionString);
                fileContents = ReplaceOrAppend(assemblyFileVersionRegex, fileContents, assemblyFileVersionString);

                fileSystem.WriteAllText(assemblyInfoFile, fileContents);
            }
        }

        static string ReplaceOrAppend(Regex replaceRegex, string inputString, string replaceString)
        {
            const string assemblyAddFormat = "[assembly: {0}]";

            if (replaceRegex.IsMatch(inputString))
            {
                inputString = replaceRegex.Replace(inputString, replaceString);
            }
            else
            {
                inputString += Environment.NewLine + string.Format(assemblyAddFormat, replaceString);
            }

            return inputString;
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