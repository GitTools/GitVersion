using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GitVersion.OutputVariables;
using GitVersion.Logging;

namespace GitVersion.Extensions.VersionAssemblyInfoResources
{
    public class AssemblyInfoFileUpdater : IDisposable
    {
        private readonly List<Action> restoreBackupTasks = new List<Action>();
        private readonly List<Action> cleanupBackupTasks = new List<Action>();

        private readonly IDictionary<string, Regex> assemblyAttributeRegexes = new Dictionary<string, Regex>
        {
            {".cs", new Regex( @"(\s*\[\s*assembly:\s*(?:.*)\s*\]\s*$(\r?\n)?)", RegexOptions.Multiline) },
            {".fs", new Regex( @"(\s*\[\s*\<assembly:\s*(?:.*)\>\s*\]\s*$(\r?\n)?)", RegexOptions.Multiline) },
            {".vb", new Regex( @"(\s*\<Assembly:\s*(?:.*)\>\s*$(\r?\n)?)", RegexOptions.Multiline) },
        };

        private const string NewLine = "\r\n";

        private readonly ISet<string> assemblyInfoFileNames;
        private readonly string workingDirectory;
        private readonly VersionVariables variables;
        private readonly IFileSystem fileSystem;
        private readonly ILog log;
        private readonly bool ensureAssemblyInfo;
        private readonly TemplateManager templateManager;

        public AssemblyInfoFileUpdater(string assemblyInfoFileName, string workingDirectory, VersionVariables variables, IFileSystem fileSystem, ILog log, bool ensureAssemblyInfo) :
                this(new HashSet<string> { assemblyInfoFileName }, workingDirectory, variables, fileSystem, log, ensureAssemblyInfo)
        { }

        public AssemblyInfoFileUpdater(ISet<string> assemblyInfoFileNames, string workingDirectory, VersionVariables variables, IFileSystem fileSystem, ILog log, bool ensureAssemblyInfo)
        {
            this.assemblyInfoFileNames = assemblyInfoFileNames;
            this.workingDirectory = workingDirectory;
            this.variables = variables;
            this.fileSystem = fileSystem;
            this.log = log;
            this.ensureAssemblyInfo = ensureAssemblyInfo;

            templateManager = new TemplateManager(TemplateType.VersionAssemblyInfoResources);
        }

        public void Update()
        {
            log.Info("Updating assembly info files");

            var assemblyInfoFiles = GetAssemblyInfoFiles(workingDirectory, assemblyInfoFileNames, fileSystem, ensureAssemblyInfo).ToList();
            log.Info($"Found {assemblyInfoFiles.Count} files");

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
                var localAssemblyInfo = assemblyInfoFile.FullName;
                var backupAssemblyInfo = localAssemblyInfo + ".bak";
                fileSystem.Copy(localAssemblyInfo, backupAssemblyInfo, true);

                restoreBackupTasks.Add(() =>
                {
                    if (fileSystem.Exists(localAssemblyInfo))
                    {
                        fileSystem.Delete(localAssemblyInfo);
                    }

                    fileSystem.Move(backupAssemblyInfo, localAssemblyInfo);
                });

                cleanupBackupTasks.Add(() => fileSystem.Delete(backupAssemblyInfo));

                var originalFileContents = fileSystem.ReadAllText(localAssemblyInfo);
                var fileContents = originalFileContents;
                var appendedAttributes = false;

                if (!string.IsNullOrWhiteSpace(assemblyVersion))
                {
                    fileContents = ReplaceOrInsertAfterLastAssemblyAttributeOrAppend(assemblyVersionRegex, fileContents, assemblyVersionString, assemblyInfoFile.Extension, ref appendedAttributes);
                }

                if (!string.IsNullOrWhiteSpace(assemblyFileVersion))
                {
                    fileContents = ReplaceOrInsertAfterLastAssemblyAttributeOrAppend(assemblyFileVersionRegex, fileContents, assemblyFileVersionString, assemblyInfoFile.Extension, ref appendedAttributes);
                }

                fileContents = ReplaceOrInsertAfterLastAssemblyAttributeOrAppend(assemblyInfoVersionRegex, fileContents, assemblyInfoVersionString, assemblyInfoFile.Extension, ref appendedAttributes);

                if (appendedAttributes)
                {
                    // If we appended any attributes, put a new line after them
                    fileContents += NewLine;
                }

                if (originalFileContents != fileContents)
                {
                    fileSystem.WriteAllText(localAssemblyInfo, fileContents);
                }
            }
        }

        private string ReplaceOrInsertAfterLastAssemblyAttributeOrAppend(Regex replaceRegex, string inputString, string replaceString, string fileExtension, ref bool appendedAttributes)
        {
            var assemblyAddFormat = templateManager.GetAddFormatFor(fileExtension);

            if (replaceRegex.IsMatch(inputString))
            {
                return replaceRegex.Replace(inputString, replaceString);
            }

            if (assemblyAttributeRegexes.TryGetValue(fileExtension, out var assemblyRegex))
            {
                var assemblyMatches = assemblyRegex.Matches(inputString);
                if (assemblyMatches.Count > 0)
                {
                    var lastMatch = assemblyMatches[assemblyMatches.Count - 1];
                    var replacementString = lastMatch.Value;
                    if (!lastMatch.Value.EndsWith(NewLine)) replacementString += NewLine;
                    replacementString += string.Format(assemblyAddFormat, replaceString);
                    replacementString += NewLine;
                    return inputString.Replace(lastMatch.Value, replacementString);
                }
            }
			
            inputString += NewLine + string.Format(assemblyAddFormat, replaceString);
            appendedAttributes = true;
            return inputString;
        }

        private IEnumerable<FileInfo> GetAssemblyInfoFiles(string workingDirectory, ISet<string> assemblyInfoFileNames, IFileSystem fileSystem, bool ensureAssemblyInfo)
        {
            if (assemblyInfoFileNames != null && assemblyInfoFileNames.Any(x => !string.IsNullOrWhiteSpace(x)))
            {
                foreach (var item in assemblyInfoFileNames)
                {
                    var fullPath = Path.Combine(workingDirectory, item);

                    if (EnsureVersionAssemblyInfoFile(ensureAssemblyInfo, fileSystem, fullPath))
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

                    if (templateManager.IsSupported(assemblyInfoFile.Extension))
                    {
                        yield return assemblyInfoFile;
                    }
                }
            }
        }

        private bool EnsureVersionAssemblyInfoFile(bool ensureAssemblyInfo, IFileSystem fileSystem, string fullPath)
        {
            if (fileSystem.Exists(fullPath))
            {
                return true;
            }

            if (!ensureAssemblyInfo)
            {
                return false;
            }

            var assemblyInfoSource = templateManager.GetTemplateFor(Path.GetExtension(fullPath));

            if (!string.IsNullOrWhiteSpace(assemblyInfoSource))
            {
                var fileInfo = new FileInfo(fullPath);

                if (fileInfo.Directory != null && !fileSystem.DirectoryExists(fileInfo.Directory.FullName))
                {
                    fileSystem.CreateDirectory(fileInfo.Directory.FullName);
                }

                fileSystem.WriteAllText(fullPath, assemblyInfoSource);
                return true;
            }

            log.Warning($"No version assembly info template available to create source file '{fullPath}'");
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

        public void CommitChanges()
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
