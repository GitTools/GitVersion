using System;
using System.Collections.Generic;
using System.Linq;

namespace GitVersionTask
{
    using System.IO;
    using System.Text.RegularExpressions;
    using GitTools;
    using GitVersion;
    using GitVersion.Helpers;
    using Microsoft.Build.Framework;

    public sealed class UpdateNativeVersionHeader : GitVersionTaskBase
    {
        private TaskLogger logger;

        public UpdateNativeVersionHeader()
        {
#if DEBUG
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Launch();
            }
#endif

            logger = new TaskLogger(this);
            Logger.SetLoggers(this.LogDebug, this.LogInfo, this.LogWarning, s => this.LogError(s));
        }

        [Required]
        public string SolutionDirectory { get; set; }

        [Required]
        public string ProjectFile { get; set; }

        [Required]
        public string IntermediateOutputPath { get; set; }

        [Required]
        public ITaskItem[] CompileFiles { get; set; }

        //Only "C++" supported at this time
        [Required]
        public string Language { get; set; }

        [Output]
        public string HeaderTempFilePath { get; set; }

        public bool NoFetch { get; set; }

        public override bool Execute()
        {
            try
            {
                InnerExecute();
                return true;
            }
            catch (WarningException errorException)
            {
                logger.LogWarning(errorException.Message);
                return true;
            }
            catch (Exception exception)
            {
                logger.LogError("Error occurred: " + exception);
                return false;
            }
            finally
            {
                Logger.Reset();
            }
        }

        void InnerExecute()
        {
            TempFileTracker.DeleteTempFiles();

            InvalidFileChecker.CheckForInvalidFiles(CompileFiles, ProjectFile);

            VersionVariables versionVariables;
            if (!ExecuteCore.TryGetVersion(SolutionDirectory, out versionVariables, NoFetch, new Authentication()))
            {
                return;
            }

            CreateTempHeaderFile(versionVariables);
        }

        void CreateTempHeaderFile(VersionVariables versionVariables)
        {
            var fileExtension = GetFileExtension();
            var headerFileName = $"GitVersionTaskAssemblyInfo.g.{fileExtension}";

            if (IntermediateOutputPath == null)
            {
                headerFileName = $"AssemblyInfo_{Path.GetFileNameWithoutExtension(ProjectFile)}_{Path.GetRandomFileName()}.g.{fileExtension}";
            }

            var workingDirectory = IntermediateOutputPath ?? TempFileTracker.TempPath;

            HeaderTempFilePath = Path.Combine(workingDirectory, headerFileName);
            using (var headerFileUpdater = new NativeHeaderFileUpdater(headerFileName, workingDirectory, versionVariables, new FileSystem()))
            {
                headerFileUpdater.Update();
                headerFileUpdater.CommitChanges();
            }
        }

        string GetFileExtension()
        {
            switch (Language)
            {
                case SupportedLanguageConstants.CPLUSPLUS:
                    return SupportedLanguageConstants.FILEEXTENSION_CPLUSPLUS;
                default:
                    throw new Exception($"Unknown language detected: '{Language}'");
            }
        }
    }

    internal class NativeHeaderFileUpdater : IDisposable
    {
        readonly List<Action> restoreBackupTasks = new List<Action>();
        readonly List<Action> cleanupBackupTasks = new List<Action>();

        ISet<string> headerFileNames;
        string workingDirectory;
        VersionVariables variables;
        IFileSystem fileSystem;
        TemplateManager templateManager;


        private NativeHeaderFileUpdater()
        {
            templateManager = new TemplateManager(TemplateType.GitVersionInformationResources);
        }

        
        public NativeHeaderFileUpdater(ISet<string> headerFileNames, string workingDirectory, VersionVariables variables, IFileSystem fileSystem)
            : this()
        {
            this.headerFileNames = headerFileNames;
            this.workingDirectory = workingDirectory;
            this.variables = variables;
            this.fileSystem = fileSystem;
        }

        public NativeHeaderFileUpdater(string headerFileName, string workingDirectory, VersionVariables versionVariables, FileSystem fileSystem)
            : this(new HashSet<string> { headerFileName }, workingDirectory, versionVariables, fileSystem)
        {
        }

        public void Update()
        {
            Logger.WriteInfo("Updating assembly info files");

            var headerFiles = GetHeaderFiles(workingDirectory, headerFileNames, fileSystem).ToList();
            Logger.WriteInfo($"Found {headerFiles.Count} files");

            var assemblyVersion = variables.AssemblySemVer;
            var assemblyVersionRegex = new Regex(@"\s*#define VERSION_FULL\s*(.*)");
            var assemblyVersionString = !string.IsNullOrWhiteSpace(assemblyVersion) ? $"\r\n#define VERSION_FULL {assemblyVersion.Replace('.', ',')}\r\n" : "0,0,0,0";

            var assemblyInfoVersion = variables.InformationalVersion;
            var assemblyInfoVersionRegex = new Regex(@"\s*#define VERSION_STRING\s*(.*)");
            var assemblyInfoVersionString = $"\r\n#define VERSION_STRING VERSION_XSTR({assemblyInfoVersion}) VERSION_BLANK \"\\0\"\r\n";

            foreach (var headerFile in headerFiles)
            {
                var backupAssemblyInfo = headerFile.FullName + ".bak";
                var localAssemblyInfo = headerFile.FullName;
                fileSystem.Copy(headerFile.FullName, backupAssemblyInfo, true);

                restoreBackupTasks.Add(() =>
                {
                    if (fileSystem.Exists(localAssemblyInfo))
                    {
                        fileSystem.Delete(localAssemblyInfo);
                    }

                    fileSystem.Move(backupAssemblyInfo, localAssemblyInfo);
                });

                cleanupBackupTasks.Add(() => fileSystem.Delete(backupAssemblyInfo));

                var originalFileContents = fileSystem.ReadAllText(headerFile.FullName);
                var fileContents = originalFileContents;
                var appendedAttributes = false;

                if (!string.IsNullOrWhiteSpace(assemblyVersion))
                {
                    fileContents = ReplaceOrAppend(assemblyVersionRegex, fileContents, assemblyVersionString, headerFile.Extension, ref appendedAttributes);
                }

                //if (!string.IsNullOrWhiteSpace(headerFile))
                //{
                //    fileContents = ReplaceOrAppend(assemblyFileVersionRegex, fileContents, assemblyFileVersionString, assemblyInfoFile.Extension, ref appendedAttributes);
                //}

                fileContents = ReplaceOrAppend(assemblyInfoVersionRegex, fileContents, assemblyInfoVersionString, headerFile.Extension, ref appendedAttributes);

                if (appendedAttributes)
                {
                    // If we appended any attributes, put a new line after them
                    fileContents += Environment.NewLine;
                }

                if (originalFileContents != fileContents)
                {
                    fileSystem.WriteAllText(headerFile.FullName, fileContents);
                }
            }
        }

        internal string ReplaceOrAppend(Regex replaceRegex, string inputString, string replaceString, string fileExtension, ref bool appendedAttributes)
        {
            if (replaceRegex.IsMatch(inputString))
            {
                appendedAttributes = false;
                return replaceRegex.Replace(inputString, replaceString);
            }
            else
            {
                var assemblyAddFormat = templateManager.GetAddFormatFor(fileExtension);

                appendedAttributes = true;
                return inputString + Environment.NewLine + string.Format(assemblyAddFormat, replaceString);
            }
        }

        internal IEnumerable<FileInfo> GetHeaderFiles(string workingDirectory, ISet<string> headerFileNames, IFileSystem fileSystem)
        {
            if (headerFileNames != null && headerFileNames.Any(x => !string.IsNullOrWhiteSpace(x)))
            {
                foreach (var headerFileName in headerFileNames)
                {
                    var fullPath = Path.Combine(workingDirectory, headerFileName);

                    if (EnsureVersionHeaderFile(fileSystem, fullPath))
                    {
                        yield return new FileInfo(fullPath);
                    }
                }
            }
            else
            {
                foreach (var item in fileSystem.DirectoryGetFiles(workingDirectory, "Version.h", SearchOption.AllDirectories))
                {
                    var assemblyInfoFile = new FileInfo(item);

                    if (templateManager.IsSupported(assemblyInfoFile.Extension))
                    {
                        yield return assemblyInfoFile;
                    }
                }
            }
        }

        internal bool EnsureVersionHeaderFile(IFileSystem fileSystem, string fullPath)
        {
            if (fileSystem.Exists(fullPath))
            {
                return true;
            }

            var assemblyInfoSource = templateManager.GetTemplateFor(Path.GetExtension(fullPath));

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

            Logger.WriteWarning($"No version header template available to create source file '{fullPath}'");
            return false;
        }

        public virtual void Dispose()
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