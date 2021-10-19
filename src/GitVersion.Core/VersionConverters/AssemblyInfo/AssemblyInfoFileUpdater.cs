using System.Text.RegularExpressions;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.VersionConverters.AssemblyInfo;

public interface IAssemblyInfoFileUpdater : IVersionConverter<AssemblyInfoContext>
{
}

public sealed class AssemblyInfoFileUpdater : IAssemblyInfoFileUpdater
{
    private readonly List<Action> restoreBackupTasks = new();
    private readonly List<Action> cleanupBackupTasks = new();

    private readonly IDictionary<string, Regex> assemblyAttributeRegexes = new Dictionary<string, Regex>
    {
        {".cs", new Regex( @"(\s*\[\s*assembly:\s*(?:.*)\s*\]\s*$(\r?\n)?)", RegexOptions.Multiline) },
        {".fs", new Regex( @"(\s*\[\s*\<assembly:\s*(?:.*)\>\s*\]\s*$(\r?\n)?)", RegexOptions.Multiline) },
        {".vb", new Regex( @"(\s*\<Assembly:\s*(?:.*)\>\s*$(\r?\n)?)", RegexOptions.Multiline) },
    };

    private readonly Regex assemblyVersionRegex = new(@"AssemblyVersion(Attribute)?\s*\(.*\)\s*");
    private readonly Regex assemblyInfoVersionRegex = new(@"AssemblyInformationalVersion(Attribute)?\s*\(.*\)\s*");
    private readonly Regex assemblyFileVersionRegex = new(@"AssemblyFileVersion(Attribute)?\s*\(.*\)\s*");

    private const string NewLine = "\r\n";

    private readonly IFileSystem fileSystem;
    private readonly ILog log;
    private readonly TemplateManager templateManager;

    public AssemblyInfoFileUpdater(ILog log, IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
        this.log = log;
        this.templateManager = new TemplateManager(TemplateType.AssemblyInfo);
    }

    public void Execute(VersionVariables variables, AssemblyInfoContext context)
    {
        var assemblyInfoFiles = GetAssemblyInfoFiles(context).ToList();
        this.log.Info("Updating assembly info files");
        this.log.Info($"Found {assemblyInfoFiles.Count} files");

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
            this.fileSystem.Copy(localAssemblyInfo, backupAssemblyInfo, true);

            this.restoreBackupTasks.Add(() =>
            {
                if (this.fileSystem.Exists(localAssemblyInfo))
                {
                    this.fileSystem.Delete(localAssemblyInfo);
                }

                this.fileSystem.Move(backupAssemblyInfo, localAssemblyInfo);
            });

            this.cleanupBackupTasks.Add(() => this.fileSystem.Delete(backupAssemblyInfo));

            var originalFileContents = this.fileSystem.ReadAllText(localAssemblyInfo);
            var fileContents = originalFileContents;
            var appendedAttributes = false;

            if (!assemblyVersion.IsNullOrWhiteSpace())
            {
                fileContents = ReplaceOrInsertAfterLastAssemblyAttributeOrAppend(this.assemblyVersionRegex, fileContents, assemblyVersionString, assemblyInfoFile.Extension, ref appendedAttributes);
            }

            if (!assemblyFileVersion.IsNullOrWhiteSpace())
            {
                fileContents = ReplaceOrInsertAfterLastAssemblyAttributeOrAppend(this.assemblyFileVersionRegex, fileContents, assemblyFileVersionString, assemblyInfoFile.Extension, ref appendedAttributes);
            }

            if (!assemblyInfoVersion.IsNullOrWhiteSpace())
            {
                fileContents = ReplaceOrInsertAfterLastAssemblyAttributeOrAppend(this.assemblyInfoVersionRegex, fileContents, assemblyInfoVersionString, assemblyInfoFile.Extension, ref appendedAttributes);
            }

            if (appendedAttributes)
            {
                // If we appended any attributes, put a new line after them
                fileContents += NewLine;
            }

            if (originalFileContents != fileContents)
            {
                this.fileSystem.WriteAllText(localAssemblyInfo, fileContents);
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

        if (replaceRegex.IsMatch(inputString))
        {
            return replaceRegex.Replace(inputString, replaceString);
        }

        if (this.assemblyAttributeRegexes.TryGetValue(fileExtension, out var assemblyRegex))
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

    private IEnumerable<FileInfo> GetAssemblyInfoFiles(AssemblyInfoContext context)
    {
        var workingDirectory = context.WorkingDirectory!;
        var ensureAssemblyInfo = context.EnsureAssemblyInfo;
        var assemblyInfoFileNames = new HashSet<string>(context.AssemblyInfoFiles);

        if (assemblyInfoFileNames.Any(x => !x.IsNullOrWhiteSpace()))
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
            foreach (var item in this.fileSystem.DirectoryEnumerateFiles(workingDirectory, "AssemblyInfo.*", SearchOption.AllDirectories))
            {
                var assemblyInfoFile = new FileInfo(item);

                if (this.templateManager.IsSupported(assemblyInfoFile.Extension))
                {
                    yield return assemblyInfoFile;
                }
            }
        }
    }

    private bool EnsureVersionAssemblyInfoFile(string fullPath, bool ensureAssemblyInfo)
    {
        fullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
        if (this.fileSystem.Exists(fullPath))
        {
            return true;
        }

        if (!ensureAssemblyInfo)
        {
            return false;
        }

        var assemblyInfoSource = this.templateManager.GetTemplateFor(Path.GetExtension(fullPath));

        if (!assemblyInfoSource.IsNullOrWhiteSpace())
        {
            var fileInfo = new FileInfo(fullPath);

            if (fileInfo.Directory != null && !this.fileSystem.DirectoryExists(fileInfo.Directory.FullName))
            {
                this.fileSystem.CreateDirectory(fileInfo.Directory.FullName);
            }

            this.fileSystem.WriteAllText(fullPath, assemblyInfoSource);
            return true;
        }

        this.log.Warning($"No version assembly info template available to create source file '{fullPath}'");
        return false;
    }
}
