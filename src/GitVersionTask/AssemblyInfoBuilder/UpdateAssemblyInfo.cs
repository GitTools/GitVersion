namespace GitVersionTask
{
    using System;
    using System.IO;
    using System.Text;

    using GitVersion;

    using Microsoft.Build.Framework;

    public class UpdateAssemblyInfo : GitVersionTaskBase
    {
        TaskLogger logger;

        public UpdateAssemblyInfo()
        {
            CompileFiles = new ITaskItem[]
            {
            };
            logger = new TaskLogger(this);
            Logger.SetLoggers(this.LogInfo, this.LogWarning, s => this.LogError(s));
        }

        [Required]
        public string SolutionDirectory { get; set; }

        [Required]
        public string ProjectFile { get; set; }

        [Required]
        public string IntermediateOutputPath { get; set; }

        [Required]
        public ITaskItem[] CompileFiles { get; set; }

        [Required]
        public string RootNamespace { get; set; }

        [Output]
        public string AssemblyInfoTempFilePath { get; set; }

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
            CreateTempAssemblyInfo(versionVariables);
        }

        void CreateTempAssemblyInfo(VersionVariables versionVariables)
        {
            if (IntermediateOutputPath == null)
            {
                var tempFileName = string.Format("AssemblyInfo_{0}_{1}.g.cs", Path.GetFileNameWithoutExtension(ProjectFile), Path.GetRandomFileName());
                AssemblyInfoTempFilePath = Path.Combine(TempFileTracker.TempPath, tempFileName);
            }
            else
            {
                AssemblyInfoTempFilePath = Path.Combine(IntermediateOutputPath, "GitVersionTaskAssemblyInfo.g.cs");
            }

            var assemblyInfoBuilder = new AssemblyInfoBuilder();
            var assemblyInfo = assemblyInfoBuilder.GetAssemblyInfoText(versionVariables, RootNamespace).Trim();

            // We need to try to read the existing text first if the file exists and see if it's the same
            // This is to avoid writing when there's no differences and causing a rebuild
            try
            {
                if (File.Exists(AssemblyInfoTempFilePath))
                {
                    var content = File.ReadAllText(AssemblyInfoTempFilePath, Encoding.UTF8).Trim();
                    if (string.Equals(assemblyInfo, content, StringComparison.Ordinal))
                    {
                        return; // nothign to do as the file matches what we'd create
                    }
                }
            }
            catch (Exception)
            {
                // Something happened reading the file, try to overwrite anyway
            }

            File.WriteAllText(AssemblyInfoTempFilePath, assemblyInfo, Encoding.UTF8);
        }
    }
}