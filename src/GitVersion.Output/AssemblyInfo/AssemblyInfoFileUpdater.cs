using System.IO.Abstractions;
using System.Text.RegularExpressions;
using GitVersion.Core;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.OutputVariables;

namespace GitVersion.Output.AssemblyInfo;

internal interface IAssemblyInfoFileUpdater : IVersionConverter<AssemblyInfoContext>;

internal sealed class AssemblyInfoFileUpdater(ILogger<AssemblyInfoFileUpdater> logger, IFileSystem fileSystem) : IAssemblyInfoFileUpdater
{
    private readonly List<Action> restoreBackupTasks = [];
    private readonly List<Action> cleanupBackupTasks = [];

    private readonly Dictionary<string, Regex> assemblyAttributeRegexes = new()
    {
        [".cs"] = RegexPatterns.Output.CsharpAssemblyAttributeRegex,
        [".fs"] = RegexPatterns.Output.FsharpAssemblyAttributeRegex,
        [".vb"] = RegexPatterns.Output.VisualBasicAssemblyAttributeRegex
    };

    private const string NewLine = "\r\n";

    private readonly TemplateManager templateManager = new(TemplateType.AssemblyInfo);
    private readonly ILogger<AssemblyInfoFileUpdater> logger = logger.NotNull();
    private readonly IFileSystem fileSystem = fileSystem.NotNull();

    public void Execute(GitVersionVariables variables, AssemblyInfoContext context)
    {
        var assemblyInfoFiles = GetAssemblyInfoFiles(context).ToList();
        this.logger.LogInformation("Updating assembly info files");
        this.logger.LogInformation("Found {Count} files", assemblyInfoFiles.Count);

        var assemblyVersion = variables.AssemblySemVer;
        var assemblyVersionString = !assemblyVersion.IsNullOrWhiteSpace() ? $"AssemblyVersion(\"{assemblyVersion}\")" : null;

        var assemblyInfoVersion = variables.InformationalVersion;
        var assemblyInfoVersionString = !assemblyInfoVersion.IsNullOrWhiteSpace() ? $"AssemblyInformationalVersion(\"{assemblyInfoVersion}\")" : null;

        var assemblyFileVersion = variables.AssemblySemFileVer;
        var assemblyFileVersionString = !assemblyFileVersion.IsNullOrWhiteSpace() ? $"AssemblyFileVersion(\"{assemblyFileVersion}\")" : null;

        foreach (var assemblyInfoFile in assemblyInfoFiles)
        {
            var localAssemblyInfo = assemblyInfoFile.FullName;
            var backupAssemblyInfo = localAssemblyInfo + ".bak";
            this.fileSystem.File.Copy(localAssemblyInfo, backupAssemblyInfo, true);

            this.restoreBackupTasks.Add(() =>
            {
                if (this.fileSystem.File.Exists(localAssemblyInfo))
                {
                    this.fileSystem.File.Delete(localAssemblyInfo);
                }

                this.fileSystem.File.Move(backupAssemblyInfo, localAssemblyInfo);
            });

            this.cleanupBackupTasks.Add(() => this.fileSystem.File.Delete(backupAssemblyInfo));

            var originalFileContents = this.fileSystem.File.ReadAllText(localAssemblyInfo);
            var fileContents = originalFileContents;
            var appendedAttributes = false;

            if (!assemblyVersion.IsNullOrWhiteSpace())
            {
                fileContents = ReplaceOrInsertAfterLastAssemblyAttributeOrAppend(RegexPatterns.Output.AssemblyVersionRegex, fileContents, assemblyVersionString, assemblyInfoFile.Extension, ref appendedAttributes);
            }

            if (!assemblyFileVersion.IsNullOrWhiteSpace())
            {
                fileContents = ReplaceOrInsertAfterLastAssemblyAttributeOrAppend(RegexPatterns.Output.AssemblyFileVersionRegex, fileContents, assemblyFileVersionString, assemblyInfoFile.Extension, ref appendedAttributes);
            }

            if (!assemblyInfoVersion.IsNullOrWhiteSpace())
            {
                fileContents = ReplaceOrInsertAfterLastAssemblyAttributeOrAppend(RegexPatterns.Output.AssemblyInfoVersionRegex, fileContents, assemblyInfoVersionString, assemblyInfoFile.Extension, ref appendedAttributes);
            }

            if (appendedAttributes)
            {
                // If we appended any attributes, put a new line after them
                fileContents += NewLine;
            }

            if (originalFileContents != fileContents)
            {
                this.fileSystem.File.WriteAllText(localAssemblyInfo, fileContents);
            }
        }
        CommitChanges();
    }

    public void Dispose()
    {
        foreach (var restoreBackup in this.restoreBackupTasks)
        {
            restoreBackup();
        }

        this.cleanupBackupTasks.Clear();
        this.restoreBackupTasks.Clear();
    }

    private void CommitChanges()
    {
        foreach (var cleanupBackupTask in this.cleanupBackupTasks)
        {
            cleanupBackupTask();
        }

        this.cleanupBackupTasks.Clear();
        this.restoreBackupTasks.Clear();
    }

    private string ReplaceOrInsertAfterLastAssemblyAttributeOrAppend(Regex replaceRegex, string inputString, string? replaceString, string fileExtension, ref bool appendedAttributes)
    {
        var assemblyAddFormat = this.templateManager.GetAddFormatFor(fileExtension);

        if (replaceRegex.IsMatch(inputString) && replaceString != null)
        {
            return replaceRegex.Replace(inputString, replaceString);
        }

        if (this.assemblyAttributeRegexes.TryGetValue(fileExtension, out var assemblyRegex))
        {
            var assemblyMatches = assemblyRegex.Matches(inputString);
            if (assemblyMatches.Count > 0)
            {
                var lastMatch = assemblyMatches[^1];
                var replacementString = lastMatch.Value;
                if (!lastMatch.Value.EndsWith(NewLine)) replacementString += NewLine;
                if (assemblyAddFormat != null)
                    replacementString += string.Format(assemblyAddFormat, replaceString);
                replacementString += NewLine;
                return inputString.Replace(lastMatch.Value, replacementString);
            }
        }

        if (assemblyAddFormat != null)
            inputString += NewLine + string.Format(assemblyAddFormat, replaceString);
        appendedAttributes = true;
        return inputString;
    }

    private IEnumerable<IFileInfo> GetAssemblyInfoFiles(AssemblyInfoContext context)
    {
        var workingDirectory = context.WorkingDirectory;
        var ensureAssemblyInfo = context.EnsureAssemblyInfo;
        var assemblyInfoFileNames = new HashSet<string>(context.AssemblyInfoFiles);

        if (assemblyInfoFileNames.Any(x => !x.IsNullOrWhiteSpace()))
        {
            foreach (var item in assemblyInfoFileNames)
            {
                var fullPath = FileSystemHelper.Path.Combine(workingDirectory, item);

                if (EnsureVersionAssemblyInfoFile(fullPath, ensureAssemblyInfo))
                {
                    yield return this.fileSystem.FileInfo.New(fullPath);
                }
            }
        }
        else
        {
            foreach (var item in this.fileSystem.Directory.EnumerateFiles(workingDirectory, "AssemblyInfo.*", SearchOption.AllDirectories))
            {
                var assemblyInfoFile = this.fileSystem.FileInfo.New(item);

                if (this.templateManager.IsSupported(assemblyInfoFile.Extension))
                {
                    yield return assemblyInfoFile;
                }
            }
        }
    }

    private bool EnsureVersionAssemblyInfoFile(string fullPath, bool ensureAssemblyInfo)
    {
        fullPath = fullPath.NotNull();
        if (this.fileSystem.File.Exists(fullPath))
        {
            return true;
        }

        if (!ensureAssemblyInfo)
        {
            return false;
        }

        var assemblyInfoSource = this.templateManager.GetTemplateFor(FileSystemHelper.Path.GetExtension(fullPath));

        if (!assemblyInfoSource.IsNullOrWhiteSpace())
        {
            var fileInfo = this.fileSystem.FileInfo.New(fullPath);

            if (fileInfo.Directory != null && !this.fileSystem.Directory.Exists(fileInfo.Directory.FullName))
            {
                this.fileSystem.Directory.CreateDirectory(fileInfo.Directory.FullName);
            }

            this.fileSystem.File.WriteAllText(fullPath, assemblyInfoSource);
            return true;
        }

        this.logger.LogWarning("No version assembly info template available to create source file '{FullPath}'", fullPath);
        return false;
    }
}
