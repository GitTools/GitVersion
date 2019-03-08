namespace GitVersionTask
{
    using System;
    using System.IO;
    using GitVersion;
    using GitVersion.Helpers;

    public static class GenerateGitVersionInformation
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
                output = InnerExecute(input );
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
            if (!execute.TryGetVersion(input.SolutionDirectory, out var versionVariables, input.NoFetch, new Authentication()))
            {
                return null;
            }

            var fileWriteInfo = input.IntermediateOutputPath.GetWorkingDirectoryAndFileNameAndExtension(
                input.Language,
                input.ProjectFile,
                ( pf, ext ) => $"GitVersionInformation.g.{ext}",
                ( pf, ext ) => $"GitVersionInformation_{Path.GetFileNameWithoutExtension( pf )}_{Path.GetRandomFileName()}.g.{ext}"
                );

            var output = new Output()
            {
                GitVersionInformationFilePath = Path.Combine( fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName )
            };
            var generator = new GitVersionInformationGenerator( fileWriteInfo.FileName, fileWriteInfo.WorkingDirectory, versionVariables, new FileSystem());
            generator.Generate();

            return output;
        }

        public sealed class Input
        {
            public string SolutionDirectory { get; set; }

            public string ProjectFile { get; set; }

            public string IntermediateOutputPath { get; set; }

            public string Language { get; set; }

            public bool NoFetch { get; set; }
        }

        private static Boolean ValidateInput( this Input input )
        {
            return input != null
                && !String.IsNullOrEmpty( input.SolutionDirectory )
                && !String.IsNullOrEmpty(input.ProjectFile)
                // && !String.IsNullOrEmpty(input.IntermediateOutputPath) // This was marked as [Required] but it InnerExecute still seems to allow it to be null... ?
                && !String.IsNullOrEmpty(input.Language)
                ;
        }

        public sealed class Output
        {
            public string GitVersionInformationFilePath { get; set; }
        }
    }
}
