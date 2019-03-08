namespace GitVersionTask
{
    using System;
    using System.IO;

    using GitVersion;
    using GitVersion.Helpers;

    public static class UpdateAssemblyInfo
    {
        public static Output Execute(
            Input input
            )
        {
            if ( !input.ValidateInput() )
            {
                throw new Exception( "Invalid input." );
            }

            var logger = new TaskLogger();
            Logger.SetLoggers( logger.LogInfo, logger.LogInfo, logger.LogWarning, s => logger.LogError( s ) );

            Output output = null;
            try
            {
                output = InnerExecute( input );
            }
            catch (WarningException errorException)
            {
                logger.LogWarning(errorException.Message);
                output = new Output();
            }
            catch (Exception exception)
            {
                logger.LogError("Error occurred: " + exception);
                throw;
            }
            finally
            {
                Logger.Reset();
            }

            return output;
        }

        private static Output InnerExecute( Input input )
        {
            var execute = GitVersionTaskBase.CreateExecuteCore();

            TempFileTracker.DeleteTempFiles();

            InvalidFileChecker.CheckForInvalidFiles(input.CompileFiles, input.ProjectFile);

            if (!execute.TryGetVersion( input.SolutionDirectory, out var versionVariables, input.NoFetch, new Authentication()))
            {
                return null;
            }

            return CreateTempAssemblyInfo(input, versionVariables);
        }

        private static Output CreateTempAssemblyInfo( Input input, VersionVariables versionVariables)
        {
            var fileWriteInfo = input.IntermediateOutputPath.GetWorkingDirectoryAndFileNameAndExtension(
                input.Language,
                input.ProjectFile,
                ( pf, ext ) => $"GitVersionTaskAssemblyInfo.g.{ext}",
                ( pf, ext ) => $"AssemblyInfo_{Path.GetFileNameWithoutExtension( pf )}_{Path.GetRandomFileName()}.g.{ext}"
                );

            var output = new Output()
            {
                AssemblyInfoTempFilePath = Path.Combine( fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName )
            };

            using (var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater( fileWriteInfo.FileName, fileWriteInfo.WorkingDirectory, versionVariables, new FileSystem(), true))
            {
                assemblyInfoFileUpdater.Update();
                assemblyInfoFileUpdater.CommitChanges();
            }

            return output;
        }

        public sealed class Input
        {
            public string SolutionDirectory { get; set; }

            public string ProjectFile { get; set; }

            public string IntermediateOutputPath { get; set; }

            public String[] CompileFiles { get; set; }

            public string Language { get; set; }

            public bool NoFetch { get; set; }
        }

        private static Boolean ValidateInput(this Input input)
        {
            return input != null
                && !String.IsNullOrEmpty( input.SolutionDirectory )
                && !String.IsNullOrEmpty( input.ProjectFile )
                && !String.IsNullOrEmpty( input.IntermediateOutputPath )
                && input.CompileFiles != null
                && !String.IsNullOrEmpty( input.Language )
                ;
        }

        public sealed class Output
        {
            public string AssemblyInfoTempFilePath { get; set; }
        }
    }
}
