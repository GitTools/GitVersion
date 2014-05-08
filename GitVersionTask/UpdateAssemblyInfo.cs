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
        public bool AppendRevision { get; set; }

        public string AssemblyVersioningScheme { get; set; }

        [Required]
        public string SolutionDirectory { get; set; }

        [Required]
        public string ProjectFile { get; set; }
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
            var avs = ParseAssemblyVersioningScheme(AssemblyVersioningScheme);

            TempFileTracker.DeleteTempFiles();

            InvalidFileChecker.CheckForInvalidFiles(CompileFiles, ProjectFile);
            
            SemanticVersion semanticVersion;
            if (!VersionAndBranchFinder.TryGetVersion(SolutionDirectory, out semanticVersion))
            {
                return;
            }

            CreateTempAssemblyInfo(semanticVersion, avs);
        }

        AssemblyVersioningScheme ParseAssemblyVersioningScheme(string assemblyVersioningScheme)
        {
            if (assemblyVersioningScheme == null)
            {
                return GitVersion.AssemblyVersioningScheme.MajorMinorPatch;
            }

            AssemblyVersioningScheme avs;

            if (Enum.TryParse(assemblyVersioningScheme, true, out avs))
            {
                return avs;
            }

            throw new ErrorException(string.Format("Unexpected assembly versioning scheme '{0}'.", assemblyVersioningScheme));
        }

        void CreateTempAssemblyInfo(SemanticVersion semanticVersion, AssemblyVersioningScheme avs)
        {
            var assemblyInfoBuilder = new AssemblyInfoBuilder
                                      {
                                          SemanticVersion = semanticVersion,
                                          AssemblyVersioningScheme = avs, 
                                          AppendRevision = AppendRevision
                                      };
            var assemblyInfo = assemblyInfoBuilder.GetAssemblyInfoText();

            var tempFileName = string.Format("AssemblyInfo_{0}_{1}.g.cs", Path.GetFileNameWithoutExtension(ProjectFile), Path.GetRandomFileName());
            AssemblyInfoTempFilePath = Path.Combine(TempFileTracker.TempPath, tempFileName);
            File.WriteAllText(AssemblyInfoTempFilePath, assemblyInfo);
        }

    }
}