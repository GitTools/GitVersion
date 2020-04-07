using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.VersionConverters.AssemblyInfo
{
    public interface IAssemblyInfoFileUpdater : IVersionConverter<AssemblyInfoContext>
    {
    }

    public class AssemblyInfoFileUpdater : IAssemblyInfoFileUpdater
    {
        private readonly List<Action> restoreBackupTasks = new List<Action>();
        private readonly List<Action> cleanupBackupTasks = new List<Action>();

        private readonly IDictionary<string, Regex> assemblyAttributeRegexes = new Dictionary<string, Regex>
        {
            {".cs", new Regex( @"(\s*\[\s*assembly:\s*(?:.*)\s*\]\s*$(\r?\n)?)", RegexOptions.Multiline) },
            {".fs", new Regex( @"(\s*\[\s*\<assembly:\s*(?:.*)\>\s*\]\s*$(\r?\n)?)", RegexOptions.Multiline) },
            {".vb", new Regex( @"(\s*\<Assembly:\s*(?:.*)\>\s*$(\r?\n)?)", RegexOptions.Multiline) },
        };

        private readonly Regex assemblyVersionRegex = new Regex(@"AssemblyVersion(Attribute)?\s*\(.*\)\s*");
        private readonly Regex assemblyInfoVersionRegex = new Regex(@"AssemblyInformationalVersion(Attribute)?\s*\(.*\)\s*");
        private readonly Regex assemblyFileVersionRegex = new Regex(@"AssemblyFileVersion(Attribute)?\s*\(.*\)\s*");

        private const string NewLine = "\r\n";

        private readonly IFileSystem fileSystem;
        private readonly ILog log;
        private readonly TemplateManager templateManager;

        public AssemblyInfoFileUpdater(ILog log, IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
            this.log = log;
            templateManager = new TemplateManager(TemplateType.AssemblyInfo);
        }

        public void Execute(VersionVariables variables, AssemblyInfoContext context)
        {
            var assemblyInfoFileNames = new HashSet<string>(context.AssemblyInfoFiles);
            log.Info("Updating assembly info files");

            var assemblyInfoFiles = GetAssemblyInfoFiles(context.WorkingDirectory, assemblyInfoFileNames, context.EnsureAssemblyInfo).ToList();
            log.Info($"Found {assemblyInfoFiles.Count} files");

            var assemblyVersion = variables.AssemblySemVer;
            var assemblyVersionString = !string.IsNullOrWhiteSpace(assemblyVersion) ? $"AssemblyVersion(\"{assemblyVersion}\")" : null;

            var assemblyInfoVersion = variables.InformationalVersion;
            var assemblyInfoVersionString = !string.IsNullOrWhiteSpace(assemblyInfoVersion) ? $"AssemblyInformationalVersion(\"{assemblyInfoVersion}\")" : null;

            var assemblyFileVersion = variables.AssemblySemFileVer;
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

                if (!string.IsNullOrWhiteSpace(assemblyInfoVersion))
                {
                    fileContents = ReplaceOrInsertAfterLastAssemblyAttributeOrAppend(assemblyInfoVersionRegex, fileContents, assemblyInfoVersionString, assemblyInfoFile.Extension, ref appendedAttributes);
                }

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
            CommitChanges();
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

        private void CommitChanges()
        {
            foreach (var cleanupBackupTask in cleanupBackupTasks)
            {
                cleanupBackupTask();
            }

            cleanupBackupTasks.Clear();
            restoreBackupTasks.Clear();
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

        private IEnumerable<FileInfo> GetAssemblyInfoFiles(string workingDirectory, ISet<string> assemblyInfoFileNames, bool ensureAssemblyInfo)
        {
            if (assemblyInfoFileNames != null && assemblyInfoFileNames.Any(x => !string.IsNullOrWhiteSpace(x)))
            {
                foreach (var item in assemblyInfoFileNames)
                {
                    var fullPath = Path.Combine(workingDirectory, item);

                    if (EnsureVersionAssemblyInfoFile(fullPath, ensureAssemblyInfo))
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

        private bool EnsureVersionAssemblyInfoFile(string fullPath, bool ensureAssemblyInfo)
        {
            fullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
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
    }
}
