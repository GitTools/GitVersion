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
                Logger.Write = message => Log.LogMessageFromText(message, MessageImportance.Normal);
                var gitDirectory = GitDirFinder.TreeWalkForGitDir(SolutionDirectory);
                VersionAndBranch versionAndBranch;
                try
                {
                    versionAndBranch = VersionCache.GetVersion(gitDirectory);
                }
                catch (ErrorException errorException)
                {
                    Log.LogError(errorException.Message);
                    return false;
                }

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
                return true;
            }
            catch (Exception exception)
            {
                Log.LogErrorFromException(exception, true, true, "ProjectFile");
                throw;
            }
            finally
            {
                Logger.Reset();
            }
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