namespace GitVersion
{
    using Helpers;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using GitVersion.VersionAssemblyInfoResources;

    // TODO: Consolidate this with GitVersionTask.UpdateAssemblyInfo. @asbjornu
    class AssemblyInfoFileUpdate : IDisposable
    {
        List<Action> restoreBackupTasks = new List<Action>();
        List<Action> cleanupBackupTasks = new List<Action>();

        public AssemblyInfoFileUpdate(Arguments args, string workingDirectory, VersionVariables variables, IFileSystem fileSystem)
        {
            if (!args.UpdateAssemblyInfo) return;

            if (args.Output != OutputType.Json)
                Logger.WriteInfo("Updating assembly info files");

            var assemblyInfoFiles = GetAssemblyInfoFiles(workingDirectory, args, fileSystem).ToList();
            Logger.WriteInfo($"Found {assemblyInfoFiles.Count} files");

            var assemblyVersion = variables.AssemblySemVer;
            var assemblyVersionRegex = new Regex(@"AssemblyVersion(Attribute)?\s*\(\s*""[^""]*""\s*\)");
            var assemblyVersionString = !string.IsNullOrWhiteSpace(assemblyVersion) ? $"AssemblyVersion(\"{assemblyVersion}\")" : null;
            var assemblyInfoVersion = variables.InformationalVersion;
            var assemblyInfoVersionRegex = new Regex(@"AssemblyInformationalVersion(Attribute)?\s*\(\s*""[^""]*""\s*\)");
            var assemblyInfoVersionString = $"AssemblyInformationalVersion(\"{assemblyInfoVersion}\")";
            var assemblyFileVersion = variables.MajorMinorPatch + ".0";
            var assemblyFileVersionRegex = new Regex(@"AssemblyFileVersion(Attribute)?\s*\(\s*""[^""]*""\s*\)");
            var assemblyFileVersionString = $"AssemblyFileVersion(\"{assemblyFileVersion}\")";

            foreach (var assemblyInfoFile in assemblyInfoFiles)
            {
                var backupAssemblyInfo = assemblyInfoFile.FullName + ".bak";
                var localAssemblyInfo = assemblyInfoFile.FullName;
                fileSystem.Copy(assemblyInfoFile.FullName, backupAssemblyInfo, true);
                restoreBackupTasks.Add(() =>
                {
                    if (fileSystem.Exists(localAssemblyInfo))
                        fileSystem.Delete(localAssemblyInfo);
                    fileSystem.Move(backupAssemblyInfo, localAssemblyInfo);
                });
                cleanupBackupTasks.Add(() => fileSystem.Delete(backupAssemblyInfo));

                var originalFileContents = fileSystem.ReadAllText(assemblyInfoFile.FullName);
                var fileContents = originalFileContents;
                var appendedAttributes = false;
                if (!string.IsNullOrWhiteSpace(assemblyVersion))
                {
                    fileContents = ReplaceOrAppend(assemblyVersionRegex, fileContents, assemblyVersionString, assemblyInfoFile.Extension, ref appendedAttributes);
                }
                fileContents = ReplaceOrAppend(assemblyInfoVersionRegex, fileContents, assemblyInfoVersionString, assemblyInfoFile.Extension, ref appendedAttributes);
                fileContents = ReplaceOrAppend(assemblyFileVersionRegex, fileContents, assemblyFileVersionString, assemblyInfoFile.Extension, ref appendedAttributes);

                if (appendedAttributes)
                {
                    // If we appended any attributes, put a new line after them
                    fileContents += Environment.NewLine;
                }
                if (originalFileContents != fileContents)
                {
                    fileSystem.WriteAllText(assemblyInfoFile.FullName, fileContents);
                }
            }
        }

        static string ReplaceOrAppend(Regex replaceRegex, string inputString, string replaceString, string fileExtension, ref bool appendedAttributes)
        {
            var assemblyAddFormat = AssemblyVersionInfoTemplates.GetAssemblyInfoAddFormatFor(fileExtension);

            if (replaceRegex.IsMatch(inputString))
            {
                inputString = replaceRegex.Replace(inputString, replaceString);
            }
            else
            {
                inputString += Environment.NewLine + string.Format(assemblyAddFormat, replaceString);
                appendedAttributes = true;
            }

            return inputString;
        }


        static IEnumerable<FileInfo> GetAssemblyInfoFiles(string workingDirectory, Arguments args, IFileSystem fileSystem)
        {
            if (args.UpdateAssemblyInfoFileName != null && args.UpdateAssemblyInfoFileName.Any(x => !string.IsNullOrWhiteSpace(x)))
            {
                foreach (var item in args.UpdateAssemblyInfoFileName)
                {
                    var fullPath = Path.Combine(workingDirectory, item);

                    if (EnsureVersionAssemblyInfoFile(args, fileSystem, fullPath))
                    {
                        yield return new FileInfo(fullPath);
                    }
                }
            }
            else
            {
                foreach (var item in fileSystem.DirectoryGetFiles(workingDirectory, "AssemblyInfo.*", SearchOption.AllDirectories))
                {
                    var assemblyInfoFile = new FileInfo(item);

                    if (AssemblyVersionInfoTemplates.IsSupported(assemblyInfoFile.Extension))
                        yield return assemblyInfoFile;
                }
            }
        }

        static bool EnsureVersionAssemblyInfoFile(Arguments arguments, IFileSystem fileSystem, string fullPath)
        {
            if (fileSystem.Exists(fullPath)) return true;

            if (!arguments.EnsureAssemblyInfo) return false;

            var assemblyInfoSource = AssemblyVersionInfoTemplates.GetAssemblyInfoTemplateFor(fullPath);
            if (!string.IsNullOrWhiteSpace(assemblyInfoSource))
            {
                var fileInfo = new FileInfo(fullPath);
                if (!fileSystem.DirectoryExists(fileInfo.Directory.FullName))
                {
                    fileSystem.CreateDirectory(fileInfo.Directory.FullName);
                }
                fileSystem.WriteAllText(fullPath, assemblyInfoSource);
                return true;
            }
            Logger.WriteWarning($"No version assembly info template available to create source file '{arguments.UpdateAssemblyInfoFileName}'");
            return false;
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