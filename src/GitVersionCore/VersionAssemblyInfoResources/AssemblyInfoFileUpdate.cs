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

        ISet<string> updateAssemblyInfoFileName;
        string workingDirectory;
        VersionVariables variables;
        IFileSystem fileSystem;
        bool ensureAssemblyInfo;

        public AssemblyInfoFileUpdate(ISet<string> updateAssemblyInfoFileName, string workingDirectory, VersionVariables variables, IFileSystem fileSystem, bool ensureAssemblyInfo)
        {
            this.updateAssemblyInfoFileName = updateAssemblyInfoFileName;
            this.workingDirectory = workingDirectory;
            this.variables = variables;
            this.fileSystem = fileSystem;
            this.ensureAssemblyInfo = ensureAssemblyInfo;
        }

        public void Update()
        {
            Logger.WriteInfo("Updating assembly info files");

            var assemblyInfoFiles = GetAssemblyInfoFiles(workingDirectory, updateAssemblyInfoFileName, fileSystem, ensureAssemblyInfo).ToList();
            Logger.WriteInfo($"Found {assemblyInfoFiles.Count} files");

            var assemblyVersion = variables.AssemblySemVer;
            var assemblyVersionRegex = new Regex(@"AssemblyVersion(Attribute)?\s*\(.*\)\s*");
            var assemblyVersionString = !string.IsNullOrWhiteSpace(assemblyVersion) ? $"AssemblyVersion(\"{assemblyVersion}\")" : null;
            var assemblyInfoVersion = variables.InformationalVersion;
            var assemblyInfoVersionRegex = new Regex(@"AssemblyInformationalVersion(Attribute)?\s*\(.*\)\s*");
            var assemblyInfoVersionString = $"AssemblyInformationalVersion(\"{assemblyInfoVersion}\")";
            var assemblyFileVersion = variables.AssemblySemFileVer;
            var assemblyFileVersionRegex = new Regex(@"AssemblyFileVersion(Attribute)?\s*\(.*\)\s*");
            var assemblyFileVersionString = !string.IsNullOrWhiteSpace(assemblyFileVersion) ? $"AssemblyFileVersion(\"{assemblyFileVersion}\")" : null;

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
                if (!string.IsNullOrWhiteSpace(assemblyFileVersion))
                {
                    fileContents = ReplaceOrAppend(assemblyFileVersionRegex, fileContents, assemblyFileVersionString, assemblyInfoFile.Extension, ref appendedAttributes);
                }
                fileContents = ReplaceOrAppend(assemblyInfoVersionRegex, fileContents, assemblyInfoVersionString, assemblyInfoFile.Extension, ref appendedAttributes);

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


        static IEnumerable<FileInfo> GetAssemblyInfoFiles(string workingDirectory, ISet<string> updateAssemblyInfoFileName, IFileSystem fileSystem, bool ensureAssemblyInfo)
        {
            if (updateAssemblyInfoFileName != null && updateAssemblyInfoFileName.Any(x => !string.IsNullOrWhiteSpace(x)))
            {
                foreach (var item in updateAssemblyInfoFileName)
                {
                    var fullPath = Path.Combine(workingDirectory, item);

                    if (EnsureVersionAssemblyInfoFile(ensureAssemblyInfo, fileSystem, fullPath, updateAssemblyInfoFileName))
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

        static bool EnsureVersionAssemblyInfoFile(bool ensureAssemblyInfo, IFileSystem fileSystem, string fullPath, ISet<string> updateAssemblyInfoFileName)
        {
            if (fileSystem.Exists(fullPath)) return true;

            if (!ensureAssemblyInfo) return false;

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
            Logger.WriteWarning($"No version assembly info template available to create source file '{updateAssemblyInfoFileName}'");
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