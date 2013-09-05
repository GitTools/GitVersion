using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GitFlowVersion;
using LibGit2Sharp;
using Mono.Cecil;

public class ModuleWeaver
{
    public Action<string> LogInfo;
    public Action<string> LogWarning;
    public ModuleDefinition ModuleDefinition;
    public string SolutionDirectoryPath;
    public string AddinDirectoryPath;
    public string AssemblyFilePath;
    static bool isPathSet;
    FormatStringTokenResolver formatStringTokenResolver;
    string assemblyInfoVersion;
    Version assemblyVersion;
    bool dotGitDirExists;

    public ModuleWeaver()
    {
        LogInfo = s => { };
        LogWarning = s => { };
        formatStringTokenResolver = new FormatStringTokenResolver();
    }

    public void Execute()
    {
        SetSearchPath();
        var customAttributes = ModuleDefinition.Assembly.CustomAttributes;

        var gitDir = GitDirFinder.TreeWalkForGitDir(SolutionDirectoryPath);
        if (gitDir == null)
        {
            LogWarning("No .git directory found.");
            return;
        }
        dotGitDirExists = true;

        using (var repo = GetRepo(gitDir))
        {
            var branch = repo.Head;
            if (branch.Tip == null)
            {
                LogWarning("No Tip found. Has repo been initialize?");
                return;
            }
            var semanticVersion = GetSemanticVersion(repo);
            SetAssemblyVersion(semanticVersion);

            ModuleDefinition.Assembly.Name.Version = assemblyVersion;

            var customAttribute = customAttributes.FirstOrDefault(x => x.AttributeType.Name == "AssemblyInformationalVersionAttribute");
            if (customAttribute != null)
            {
                assemblyInfoVersion = (string) customAttribute.ConstructorArguments[0].Value;
                var replaceTokens = formatStringTokenResolver.ReplaceTokens(assemblyInfoVersion, repo, semanticVersion);
                assemblyInfoVersion = string.Format("{0}.{1}.{2} {3}", semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch, replaceTokens);
                customAttribute.ConstructorArguments[0] = new CustomAttributeArgument(ModuleDefinition.TypeSystem.String, assemblyInfoVersion);
            }
            else
            {
                var versionAttribute = GetVersionAttribute();
                var constructor = ModuleDefinition.Import(versionAttribute.Methods.First(x => x.IsConstructor));
                customAttribute = new CustomAttribute(constructor);

                string prereleaseString = "";

                if (semanticVersion.Stage != Stage.Final)
                {
                    prereleaseString = "-" + semanticVersion.Stage + semanticVersion.PreRelease;
                }

                var versionPrefix = string.Format("{0}.{1}.{2}{3}", semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch, prereleaseString);

                if (repo.IsClean())
                {
                    assemblyInfoVersion = string.Format("{0} Head:'{1}' Sha:{2}", versionPrefix, repo.Head.Name, branch.Tip.Sha);
                }
                else
                {
                    assemblyInfoVersion = string.Format("{0} Head:'{1}' Sha:{2} HasPendingChanges", versionPrefix, repo.Head.Name, branch.Tip.Sha);
                }
                customAttribute.ConstructorArguments.Add(new CustomAttributeArgument(ModuleDefinition.TypeSystem.String, assemblyInfoVersion));
                customAttributes.Add(customAttribute);
            }

            OutputVersionToBuildServer(semanticVersion);
        }
    }

    void OutputVersionToBuildServer(SemanticVersion semanticVersion)
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION")))
        {
            return;
        }

        string prereleaseString = "";

        if (semanticVersion.Stage != Stage.Final)
        {
            prereleaseString = "-" + semanticVersion.Stage;

            if (!string.IsNullOrEmpty(semanticVersion.Suffix))
            {
                prereleaseString += semanticVersion.Suffix;
            }
            else
            {
                prereleaseString += semanticVersion.PreRelease;
            }
        }

        Console.Out.WriteLine("##teamcity[buildNumber '{0}.{1}.{2}{3}']", semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch, prereleaseString);
    }

    public virtual SemanticVersion GetSemanticVersion(Repository repo)
    {
        var versionForRepositoryFinder = new VersionForRepositoryFinder();
        return versionForRepositoryFinder.GetVersion(repo);
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

    static Repository GetRepo(string gitDir)
    {
        try
        {
            return new Repository(gitDir);
        }
        catch (Exception exception)
        {
            if (exception.Message.Contains("LibGit2Sharp.Core.NativeMethods") || exception.Message.Contains("FilePathMarshaler"))
            {
                throw new WeavingException("Restart of Visual Studio required due to update of 'GitFlowVersion.Fody'.");
            }
            throw;
        }
    }

    void SetSearchPath()
    {
        if (isPathSet)
        {
            return;
        }
        isPathSet = true;
        var nativeBinaries = Path.Combine(AddinDirectoryPath, "NativeBinaries", GetProcessorArchitecture());
        var existingPath = Environment.GetEnvironmentVariable("PATH");
        var newPath = string.Concat(nativeBinaries, Path.PathSeparator, existingPath);
        Environment.SetEnvironmentVariable("PATH", newPath);
    }

    static string GetProcessorArchitecture()
    {
        if (Environment.Is64BitProcess)
        {
            return "amd64";
        }
        return "x86";
    }

    TypeDefinition GetVersionAttribute()
    {
        var msCoreLib = ModuleDefinition.AssemblyResolver.Resolve("mscorlib");
        var msCoreAttribute = msCoreLib.MainModule.Types.FirstOrDefault(x => x.Name == "AssemblyInformationalVersionAttribute");
        if (msCoreAttribute != null)
        {
            return msCoreAttribute;
        }
        var systemRuntime = ModuleDefinition.AssemblyResolver.Resolve("System.Runtime");
        return systemRuntime.MainModule.Types.First(x => x.Name == "AssemblyInformationalVersionAttribute");
    }

    public void AfterWeaving()
    {
        if (!dotGitDirExists)
        {
            return;
        }
        var verPatchPath = Path.Combine(AddinDirectoryPath, "verpatch.exe");
        var arguments = string.Format("{0} /pv \"{1}\" /high /va {2}", AssemblyFilePath, assemblyInfoVersion, assemblyVersion);
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
            if (!process.WaitForExit(1000))
            {
                var timeoutMessage = string.Format("Failed to apply product version to Win32 resources in 1 second.\r\nFailed command: {0} {1}", verPatchPath, arguments);
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
}