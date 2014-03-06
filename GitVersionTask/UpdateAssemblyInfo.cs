namespace GitVersionTask
{
    using System;
    using System.IO;
    using GitVersion;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Logger = GitVersion.Logger;

    public class UpdateAssemblyInfo : Task
    {

        public bool SignAssembly { get; set; }

        [Required]
        public string SolutionDirectory { get; set; }

        [Required]
        public string ProjectFile { get; set; }
        [Required]
        public string AssemblyName { get; set; }
        [Required]
        public ITaskItem[] CompileFiles { get; set; }

        [Output]
        public string AssemblyInfoTempFilePath { get; set; }

        TaskLogger logger;

        public UpdateAssemblyInfo()
        {
            CompileFiles = new ITaskItem[] {};
            logger = new TaskLogger(this);
            Logger.WriteInfo = this.LogInfo;
            Logger.WriteWarning = this.LogWarning;
        }

        public override bool Execute()
        {
            try
            {
                InnerExecute();
                return true;
            }
            catch (ErrorException errorException)
            {
                logger.LogError(errorException.Message);
                return false;
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

        public void InnerExecute()
        {
            TempFileTracker.DeleteTempFiles();

            InvalidFileChecker.CheckForInvalidFiles(CompileFiles, ProjectFile);
            
            VersionAndBranch versionAndBranch;
            if (!VersionAndBranchFinder.TryGetVersion(SolutionDirectory, out versionAndBranch))
            {
                return;
            }

            CreateTempAssemblyInfo(versionAndBranch);

        }


        void CreateTempAssemblyInfo(VersionAndBranch versionAndBranch)
        {
            var assemblyInfoBuilder = new AssemblyInfoBuilder
                                      {
                                          VersionAndBranch = versionAndBranch,
                                          SignAssembly = SignAssembly,
                                          AssemblyName = AssemblyName
                                      };
            var assemblyInfo = assemblyInfoBuilder.GetAssemblyInfoText();

            var tempFileName = string.Format("AssemblyInfo_{0}_{1}.g.cs", Path.GetFileNameWithoutExtension(ProjectFile), Path.GetRandomFileName());
            AssemblyInfoTempFilePath = Path.Combine(TempFileTracker.TempPath, tempFileName);
            File.WriteAllText(AssemblyInfoTempFilePath, assemblyInfo);
        }


    }
}