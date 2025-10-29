using System.IO.Abstractions;
using System.Xml.Linq;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.Output.AssemblyInfo;

internal interface IProjectFileUpdater : IVersionConverter<AssemblyInfoContext>;

internal sealed class ProjectFileUpdater(ILog log, IFileSystem fileSystem) : IProjectFileUpdater
{
    internal const string AssemblyVersionElement = "AssemblyVersion";

    private const int DefaultMaxRecursionDepth = 255;
    private const string FileVersionElement = "FileVersion";
    private const string InformationalVersionElement = "InformationalVersion";
    private const string VersionElement = "Version";

    private readonly List<Action> restoreBackupTasks = [];
    private readonly List<Action> cleanupBackupTasks = [];

    public void Execute(GitVersionVariables variables, AssemblyInfoContext context)
    {
        if (context.EnsureAssemblyInfo)
            throw new WarningException($"Configuration setting {nameof(context.EnsureAssemblyInfo)} is not valid when updating project files!");

        var projectFilesToUpdate = GetProjectFiles(context).ToList();

        var assemblyVersion = variables.AssemblySemVer;
        var assemblyInfoVersion = variables.InformationalVersion;
        var assemblyFileVersion = variables.AssemblySemFileVer;
        var packageVersion = variables.SemVer;

        foreach (var projectFile in projectFilesToUpdate)
        {
            var localProjectFile = projectFile.FullName;

            var originalFileContents = fileSystem.File.ReadAllText(localProjectFile);
            XElement fileXml;
            try
            {
                fileXml = XElement.Parse(originalFileContents);
            }
            catch (XmlException e)
            {
                throw new XmlException($"Unable to parse file as xml: {localProjectFile}", e);
            }

            if (!CanUpdateProjectFile(fileXml))
            {
                log.Warning($"Unable to update file: {localProjectFile}");
                continue;
            }

            log.Debug($"Update file: {localProjectFile}");

            var backupProjectFile = localProjectFile + ".bak";
            fileSystem.File.Copy(localProjectFile, backupProjectFile, true);

            this.restoreBackupTasks.Add(() =>
            {
                if (fileSystem.File.Exists(localProjectFile))
                {
                    fileSystem.File.Delete(localProjectFile);
                }

                fileSystem.File.Move(backupProjectFile, localProjectFile);
            });

            this.cleanupBackupTasks.Add(() => fileSystem.File.Delete(backupProjectFile));

            if (!assemblyVersion.IsNullOrWhiteSpace())
            {
                UpdateProjectVersionElement(fileXml, AssemblyVersionElement, assemblyVersion);
            }

            if (!assemblyFileVersion.IsNullOrWhiteSpace())
            {
                UpdateProjectVersionElement(fileXml, FileVersionElement, assemblyFileVersion);
            }

            if (!assemblyInfoVersion.IsNullOrWhiteSpace())
            {
                UpdateProjectVersionElement(fileXml, InformationalVersionElement, assemblyInfoVersion);
            }

            if (!packageVersion.IsNullOrWhiteSpace())
            {
                UpdateProjectVersionElement(fileXml, VersionElement, packageVersion);
            }

            var outputXmlString = fileXml.ToString();
            if (originalFileContents != outputXmlString)
            {
                fileSystem.File.WriteAllText(localProjectFile, outputXmlString);
            }
        }

        CommitChanges();
    }

    public bool CanUpdateProjectFile(XElement xmlRoot)
    {
        if (xmlRoot.Name != "Project")
        {
            log.Warning("Invalid project file specified, root element must be <Project>.");
            return false;
        }

        var sdkAttribute = xmlRoot.Attribute("Sdk");
        if (sdkAttribute?.Value.StartsWith("Microsoft.NET.Sdk") != true &&
            sdkAttribute?.Value.StartsWith("Microsoft.Build.Sql") != true)
        {
            log.Warning($"Specified project file Sdk ({sdkAttribute?.Value}) is not supported, please ensure the project sdk starts with 'Microsoft.NET.Sdk' or 'Microsoft.Build.Sql'");
            return false;
        }

        var propertyGroups = xmlRoot.Descendants("PropertyGroup").ToList();
        if (propertyGroups.Count == 0)
        {
            log.Warning("Unable to locate any <PropertyGroup> elements in specified project file. Are you sure it is in a correct format?");
            return false;
        }

        var lastGenerateAssemblyInfoElement = propertyGroups.SelectMany(s => s.Elements("GenerateAssemblyInfo")).LastOrDefault();
        if (lastGenerateAssemblyInfoElement == null || (bool)lastGenerateAssemblyInfoElement) return true;
        log.Warning("Project file specifies <GenerateAssemblyInfo>false</GenerateAssemblyInfo>: versions set in this project file will not affect the output artifacts.");
        return false;
    }

    internal static void UpdateProjectVersionElement(XElement xmlRoot, string versionElement, string versionValue)
    {
        var propertyGroups = xmlRoot.Descendants("PropertyGroup").ToList();

        var propertyGroupToModify = propertyGroups.LastOrDefault(l => l.Element(versionElement) != null)
                                    ?? propertyGroups[0];

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

    private IEnumerable<IFileInfo> GetProjectFiles(AssemblyInfoContext context)
    {
        var workingDirectory = context.WorkingDirectory;
        var assemblyInfoFileNames = new HashSet<string>(context.AssemblyInfoFiles);

        if (assemblyInfoFileNames.Any(x => !x.IsNullOrWhiteSpace()))
        {
            foreach (var item in assemblyInfoFileNames)
            {
                var fullPath = FileSystemHelper.Path.Combine(workingDirectory, item);

                if (fileSystem.File.Exists(fullPath))
                {
                    yield return fileSystem.FileInfo.New(fullPath);
                }
                else
                {
                    log.Warning($"Specified file {fullPath} was not found and will not be updated.");
                }
            }
        }
        else
        {
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                MaxRecursionDepth = DefaultMaxRecursionDepth
            };
            var projectFiles = fileSystem.Directory
                .EnumerateFiles(workingDirectory, "*proj", options)
                .Where(IsSupportedProjectFile);

            foreach (var projectFile in projectFiles)
            {
                yield return fileSystem.FileInfo.New(projectFile);
            }
        }
    }

    private static bool IsSupportedProjectFile(string fileName)
    {
        if (fileName.IsNullOrEmpty())
        {
            return false;
        }

        return fileName.EndsWith(".csproj") ||
               fileName.EndsWith(".fsproj") ||
               fileName.EndsWith(".vbproj") ||
               fileName.EndsWith(".sqlproj");
    }
}
