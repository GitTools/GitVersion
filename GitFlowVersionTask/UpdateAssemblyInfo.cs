namespace GitFlowVersionTask
{
    using System;
    using System.IO;
    using GitFlowVersion;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Logger = GitFlowVersion.Logger;

    public class UpdateAssemblyInfo : Task
    {
        static string tempPath;

        [Required]
        public bool SignAssembly { get; set; }

        [Required]
        public string SolutionDirectory { get; set; }

        [Required]
        public string ProjectFile { get; set; }
        [Required]
        public ITaskItem[] CompileFiles { get; set; }

        static UpdateAssemblyInfo()
        {
            tempPath = Path.Combine(Path.GetTempPath(), "GitFlowVersionTask");
            Directory.CreateDirectory(tempPath);
        }

        [Output]
        public string AssemblyInfoTempFilePath { get; set; }

        public override bool Execute()
        {
            DeleteTempFiles();
            try
            {

                Logger.WriteInfo = message => Log.LogMessageFromText(message, MessageImportance.Normal);
                var gitDirectory = GitDirFinder.TreeWalkForGitDir(SolutionDirectory);
                if (string.IsNullOrEmpty(gitDirectory))
                {
                    if (TeamCity.IsRunningInBuildAgent()) //fail the build if we're on a TC build agent
                    {
                        Log.LogError("Failed to find .git directory on agent. Please make sure agent checkout mode is enabled for you VCS roots - http://confluence.jetbrains.com/display/TCD8/VCS+Checkout+Mode");
                        return false;
                    }

                    Log.LogWarning("No .git directory found in solution path '{0}'. This means the assembly may not be versioned correctly. To fix this warning either clone the repository using git or remove the `GitFlowVersion.Fody` nuget package. To temporarily work around this issue add a AssemblyInfo.cs with an appropriate `AssemblyVersionAttribute`.",SolutionDirectory);

                    return true;
                }

                var versionAndBranch = VersionCache.GetVersion(gitDirectory);

                foreach (var buildParameters in TeamCity.GenerateBuildLogOutput(versionAndBranch))
                {
                    Log.LogWarning(buildParameters,new object[]{});
                }
                CreateTempAssemblyInfo(versionAndBranch);

                return true;
            }
            catch (ErrorException errorException)
            {
                Log.LogError(errorException.Message);
                return false;
            }
            catch (Exception exception)
            {
                Log.LogErrorFromException(exception, true, true, "ProjectFile");
                return false;
            }
            finally
            {
                Logger.Reset();
            }
        }

        void CreateTempAssemblyInfo(VersionAndBranch versionAndBranch)
        {
            var assemblyVersion = GetAssemblyVersion(versionAndBranch);
            var assemblyInfo = string.Format(@"
using System.Reflection;
[assembly: AssemblyVersion(""{0}"")]
[assembly: AssemblyFileVersion(""{0}"")]
[assembly: AssemblyInformationalVersion(""{1}"")]
", assemblyVersion, versionAndBranch.ToLongString());

            var tempFileName = "AssemblyInfo_" + Path.GetFileNameWithoutExtension(ProjectFile) + "_" + Path.GetRandomFileName();
            AssemblyInfoTempFilePath = Path.Combine(tempPath, tempFileName);
            File.WriteAllText(AssemblyInfoTempFilePath, assemblyInfo);
        }

        Version GetAssemblyVersion(VersionAndBranch versionAndBranch)
        {
            var semanticVersion = versionAndBranch.Version;
            if (SignAssembly)
            {
                // for strong named we don't want to include the patch to avoid binding redirect issues
                return new Version(semanticVersion.Major, semanticVersion.Minor, 0, 0);
            }
            // for non strong named we want to include the patch
            return new Version(semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch, 0);
        }

        static void DeleteTempFiles()
        {
            foreach (var file in Directory.GetFiles(tempPath))
            {
                if (File.GetLastWriteTime(file) < DateTime.Now.AddHours(-1))
                {
                    File.Delete(file);
                }
            }
        }
    }
}