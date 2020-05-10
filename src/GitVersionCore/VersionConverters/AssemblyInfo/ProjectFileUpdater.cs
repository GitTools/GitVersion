using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.VersionConverters.AssemblyInfo
{
    public interface IProjectFileUpdater : IVersionConverter<AssemblyInfoContext>
    {
    }

    public class ProjectFileUpdater : IProjectFileUpdater
    {
        internal const string AssemblyVersionElement = "AssemblyVersion";
        internal const string FileVersionElement = "FileVersion";
        internal const string InformationalVersionElement = "InformationalVersion";

        private readonly List<Action> restoreBackupTasks = new List<Action>();
        private readonly List<Action> cleanupBackupTasks = new List<Action>();

        private readonly IFileSystem fileSystem;
        private readonly ILog log;

        public ProjectFileUpdater(ILog log, IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
            this.log = log;
        }

        public void Execute(VersionVariables variables, AssemblyInfoContext context)
        {
            if (context.EnsureAssemblyInfo)
                throw new WarningException($"Configuration setting {nameof(context.EnsureAssemblyInfo)} is not valid when updating project files!");

            var projectFilesToUpdate = GetProjectFiles(context).ToList();

            var assemblyVersion = variables.AssemblySemVer;
            var assemblyInfoVersion = variables.InformationalVersion;
            var assemblyFileVersion = variables.AssemblySemFileVer;

            foreach (var projectFile in projectFilesToUpdate)
            {
                var localProjectFile = projectFile.FullName;

                var originalFileContents = fileSystem.ReadAllText(localProjectFile);
                var fileXml = XElement.Parse(originalFileContents);

                if (!CanUpdateProjectFile(fileXml))
                {
                    log.Warning($"Unable to update file: {localProjectFile}");
                    continue;
                }

                var backupProjectFile = localProjectFile + ".bak";
                fileSystem.Copy(localProjectFile, backupProjectFile, true);

                restoreBackupTasks.Add(() =>
                {
                    if (fileSystem.Exists(localProjectFile))
                    {
                        fileSystem.Delete(localProjectFile);
                    }

                    fileSystem.Move(backupProjectFile, localProjectFile);
                });

                cleanupBackupTasks.Add(() => fileSystem.Delete(backupProjectFile));

                if (!string.IsNullOrWhiteSpace(assemblyVersion))
                {
                    UpdateProjectVersionElement(fileXml, AssemblyVersionElement, assemblyVersion);
                }

                if (!string.IsNullOrWhiteSpace(assemblyFileVersion))
                {
                    UpdateProjectVersionElement(fileXml, FileVersionElement, assemblyFileVersion);
                }

                if (!string.IsNullOrWhiteSpace(assemblyInfoVersion))
                {
                    UpdateProjectVersionElement(fileXml, InformationalVersionElement, assemblyInfoVersion);
                }

                var outputXmlString = fileXml.ToString();
                if (originalFileContents != outputXmlString)
                {
                    fileSystem.WriteAllText(localProjectFile, outputXmlString);
                }
            }

            CommitChanges();
        }

        internal bool CanUpdateProjectFile(XElement xmlRoot)
        {
            if (xmlRoot.Name != "Project")
            {
                log.Warning($"Invalid project file specified, root element must be <Project>.");
                return false;
            }

            var supportedSdk = "Microsoft.NET.Sdk";
            var sdkAttribute = xmlRoot.Attribute("Sdk");
            if (sdkAttribute == null || sdkAttribute.Value != supportedSdk)
            {
                log.Warning($"Specified project file Sdk ({sdkAttribute?.Value}) is not supported, please ensure the project sdk is {supportedSdk}.");
                return false;
            }

            var propertyGroups = xmlRoot.Descendants("PropertyGroup").ToList();
            if (!propertyGroups.Any())
            {
                log.Warning("Unable to locate any <PropertyGroup> elements in specified project file. Are you sure it is in a correct format?");
                return false;
            }

            var lastGenerateAssemblyInfoElement = propertyGroups.SelectMany(s => s.Elements("GenerateAssemblyInfo")).LastOrDefault();
            if (lastGenerateAssemblyInfoElement != null && (bool)lastGenerateAssemblyInfoElement == false)
            {
                log.Warning($"Project file specifies <GenerateAssemblyInfo>false</GenerateAssemblyInfo>: versions set in this project file will not affect the output artifacts.");
                return false;
            }

            return true;
        }

        internal void UpdateProjectVersionElement(XElement xmlRoot, string versionElement, string versionValue)
        {
            var propertyGroups = xmlRoot.Descendants("PropertyGroup").ToList();

            var propertyGroupToModify = propertyGroups.LastOrDefault(l => l.Element(versionElement) != null)
                ?? propertyGroups.First();

            var versionXmlElement = propertyGroupToModify.Elements(versionElement).LastOrDefault();
            if (versionXmlElement != null)
            {
                versionXmlElement.Value = versionValue;
            }
            else
            {
                propertyGroupToModify.SetElementValue(versionElement, versionValue);
            }
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

        private IEnumerable<FileInfo> GetProjectFiles(AssemblyInfoContext context)
        {
            var workingDirectory = context.WorkingDirectory;
            var assemblyInfoFileNames = new HashSet<string>(context.AssemblyInfoFiles);

            if (assemblyInfoFileNames.Any(x => !string.IsNullOrWhiteSpace(x)))
            {
                foreach (var item in assemblyInfoFileNames)
                {
                    var fullPath = Path.Combine(workingDirectory, item);

                    if (fileSystem.Exists(fullPath))
                    {
                        yield return new FileInfo(fullPath);
                    }
                    else
                    {
                        log.Warning($"Specified file {fullPath} was not found and will not be updated.");
                    }
                }
            }
            else
            {
                foreach (var item in fileSystem.DirectoryEnumerateFiles(workingDirectory, "*", SearchOption.AllDirectories).Where(IsSupportedProjectFile))
                {
                    var assemblyInfoFile = new FileInfo(item);

                    yield return assemblyInfoFile;
                }
            }
        }

        private bool IsSupportedProjectFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            return fileName.EndsWith(".csproj") ||
                   fileName.EndsWith(".fsproj") ||
                   fileName.EndsWith(".vbproj");
        }
    }
}
