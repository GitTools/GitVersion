using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GitFlowVersion;
using Mono.Cecil;

public class ModuleWeaver : IDisposable
{

    public Action<string> LogInfo;
    public Action<string> LogWarning;
    public ModuleDefinition ModuleDefinition;
    public string SolutionDirectoryPath;
    public string AddinDirectoryPath;
    public string AssemblyFilePath;
    string assemblyInfoVersion;
    Version assemblyVersion;
    bool gitDirectoryExists;

    public ModuleWeaver()
    {
        LogInfo = s => { };
        LogWarning = s => { };
    }

    public void Execute()
    {
        Logger.WriteInfo = LogInfo;
        SearchPath.SetSearchPath(AddinDirectoryPath);
        var customAttributes = ModuleDefinition.Assembly.CustomAttributes;

        var gitDirectory = GitDirFinder.TreeWalkForGitDir(SolutionDirectoryPath);

        if (string.IsNullOrEmpty(gitDirectory))
        {
            if (TeamCity.IsRunningInBuildAgent()) //fail the build if we're on a TC build agent
            {
                throw new WeavingException("Failed to find .git directory on agent. Please make sure agent checkout mode is enabled for you VCS roots - http://confluence.jetbrains.com/display/TCD8/VCS+Checkout+Mode");
            }

            LogWarning(string.Format("No .git directory found in solution path '{0}'. This means the assembly may not be versioned correctly. To fix this warning either clone the repository using git or remove the `GitFlowVersion.Fody` nuget package. To temporarily work around this issue add a AssemblyInfo.cs with an appropriate `AssemblyVersionAttribute`.", SolutionDirectoryPath));

            return;
        }

        gitDirectoryExists = true;


        var versionAndBranch = GetVersionAndBranch(gitDirectory);
        SetAssemblyVersion(versionAndBranch.Version);

        ModuleDefinition.Assembly.Name.Version = assemblyVersion;


        assemblyInfoVersion = versionAndBranch.ToLongString();


        var customAttribute = customAttributes.FirstOrDefault(x => x.AttributeType.Name == "AssemblyInformationalVersionAttribute");
        if (customAttribute == null)
        {
            var versionAttribute = ModuleDefinition.GetAssemblyInformationalVersionType();
            var constructor = ModuleDefinition.Import(versionAttribute.Methods.First(x => x.IsConstructor));
            customAttribute = new CustomAttribute(constructor);

            customAttribute.ConstructorArguments.Add(new CustomAttributeArgument(ModuleDefinition.TypeSystem.String, assemblyInfoVersion));
            customAttributes.Add(customAttribute);
        }
        else
        {
            //TODO: log warning that assemblyInfoVersion is being overwritten
            customAttribute.ConstructorArguments[0] = new CustomAttributeArgument(ModuleDefinition.TypeSystem.String, assemblyInfoVersion);
        }

        foreach (var buildParameters in TeamCity.GenerateBuildLogOutput(versionAndBranch))
        {
            LogWarning(buildParameters);
        }
    }

    public virtual VersionAndBranch GetVersionAndBranch(string gitDirectory)
    {
        try
        {
            return VersionCache.GetVersion(gitDirectory);
        }
        catch (ErrorException errorException)
        {
            throw new WeavingException(errorException.Message);
        }
    }

    void SetAssemblyVersion(SemanticVersion semanticVersion)
    {
        if (ModuleDefinition.IsStrongNamed())
        {
            // for strong named we don't want to include the patch to avoid binding redirect issues
            assemblyVersion = new Version(semanticVersion.Major, semanticVersion.Minor, 0, 0);
        }
        else
        {
            // for non strong named we want to include the patch
            assemblyVersion = new Version(semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch, 0);
        }
    }


    public void AfterWeaving()
    {
        if (!gitDirectoryExists)
        {
            return;
        }
        var verPatchPath = Path.Combine(AddinDirectoryPath, "verpatch.exe");
        var arguments = string.Format("\"{0}\" /pv \"{1}\" /high /va {2}", AssemblyFilePath, assemblyInfoVersion, assemblyVersion);
        LogInfo(string.Format("Patching version using: {0} {1}", verPatchPath, arguments));
        var startInfo = new ProcessStartInfo
                        {
                            FileName = verPatchPath,
                            Arguments = arguments,
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            WorkingDirectory = Path.GetTempPath()
                        };
        using (var process = Process.Start(startInfo))
        {
            if (!process.WaitForExit(4000))
            {
                var timeoutMessage = string.Format("Failed to apply product version to Win32 resources in 4 seconds.\r\nFailed command: {0} {1}", verPatchPath, arguments);
                throw new WeavingException(timeoutMessage);
            }

            if (process.ExitCode == 0)
            {
                return;
            }
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            var message = string.Format("Failed to apply product version to Win32 resources.\r\nOutput: {0}\r\nError: {1}", output, error);
            throw new WeavingException(message);
        }
    }


    public void Dispose()
    {
        Logger.Reset();
    }
}