using System.Xml.Linq;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.VersionConverters.AssemblyInfo;

public interface IProjectFileUpdater : IVersionConverter<AssemblyInfoContext>
{
    bool CanUpdateProjectFile(XElement xmlRoot);
}

public sealed class ProjectFileUpdater : IProjectFileUpdater
{
    internal const string AssemblyVersionElement = "AssemblyVersion";
    private const string FileVersionElement = "FileVersion";
    private const string InformationalVersionElement = "InformationalVersion";
    private const string VersionElement = "Version";

    private readonly List<Action> restoreBackupTasks = new();
    private readonly List<Action> cleanupBackupTasks = new();

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
        var packageVersion = variables.SemVer;

        foreach (var projectFile in projectFilesToUpdate)
        {
            var localProjectFile = projectFile.FullName;

            var originalFileContents = this.fileSystem.ReadAllText(localProjectFile);
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
                this.log.Warning($"Unable to update file: {localProjectFile}");
                continue;
            }

            this.log.Debug($"Update file: {localProjectFile}");

            var backupProjectFile = localProjectFile + ".bak";
            this.fileSystem.Copy(localProjectFile, backupProjectFile, true);

            this.restoreBackupTasks.Add(() =>
            {
                if (this.fileSystem.Exists(localProjectFile))
                {
                    this.fileSystem.Delete(localProjectFile);
                }

                this.fileSystem.Move(backupProjectFile, localProjectFile);
            });

            this.cleanupBackupTasks.Add(() => this.fileSystem.Delete(backupProjectFile));

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
                this.fileSystem.WriteAllText(localProjectFile, outputXmlString);
            }
        }

        CommitChanges();
    }

    public bool CanUpdateProjectFile(XElement xmlRoot)
    {
        if (xmlRoot.Name != "Project")
        {
            this.log.Warning("Invalid project file specified, root element must be <Project>.");
            return false;
        }

        var sdkAttribute = xmlRoot.Attribute("Sdk");
        if (sdkAttribute == null || !sdkAttribute.Value.StartsWith("Microsoft.NET.Sdk"))
        {
            this.log.Warning($"Specified project file Sdk ({sdkAttribute?.Value}) is not supported, please ensure the project sdk starts with 'Microsoft.NET.Sdk'");
            return false;
        }

        var propertyGroups = xmlRoot.Descendants("PropertyGroup").ToList();
        if (!propertyGroups.Any())
        {
            this.log.Warning("Unable to locate any <PropertyGroup> elements in specified project file. Are you sure it is in a correct format?");
            return false;
        }

        var lastGenerateAssemblyInfoElement = propertyGroups.SelectMany(s => s.Elements("GenerateAssemblyInfo")).LastOrDefault();
        if (lastGenerateAssemblyInfoElement != null && (bool)lastGenerateAssemblyInfoElement == false)
        {
            this.log.Warning("Project file specifies <GenerateAssemblyInfo>false</GenerateAssemblyInfo>: versions set in this project file will not affect the output artifacts.");
            return false;
        }

        return true;
    }

    internal static void UpdateProjectVersionElement(XElement xmlRoot, string versionElement, string versionValue)
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

    private IEnumerable<FileInfo> GetProjectFiles(AssemblyInfoContext context)
    {
        var workingDirectory = context.WorkingDirectory;
        var assemblyInfoFileNames = new HashSet<string>(context.AssemblyInfoFiles);

        if (assemblyInfoFileNames.Any(x => !x.IsNullOrWhiteSpace()))
        {
            foreach (var item in assemblyInfoFileNames)
            {
                var fullPath = PathHelper.Combine(workingDirectory, item);

                if (this.fileSystem.Exists(fullPath))
                {
                    yield return new FileInfo(fullPath);
                }
                else
                {
                    this.log.Warning($"Specified file {fullPath} was not found and will not be updated.");
                }
            }
        }
        else
        {
            foreach (var item in this.fileSystem.DirectoryEnumerateFiles(workingDirectory, "*", SearchOption.AllDirectories).Where(IsSupportedProjectFile))
            {
                var assemblyInfoFile = new FileInfo(item);

                yield return assemblyInfoFile;
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
               fileName.EndsWith(".vbproj");
    }
}
