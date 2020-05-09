using GitVersion.Extensions;
using GitVersion.Logging;
using System;
using System.Threading.Tasks;

namespace GitVersion
{
    /// <summary>
    /// The purpose of this class is to wrap execution of each subcommand's logic,
    /// to have a centralised location to handle exceptions, and log those exceptions,
    /// based on the global option arguments used to control log output.
    /// </summary>
    /// <remarks>This is evolving in a similar way to <see cref="GitVersionExecutor"/> but for executing the new commands, and using the GlobalOptions for the new cli instead.
    /// Allowing <see cref="GitVersionExecutor"/> to serve as a reference until the port is finished.
    ///</remarks>
    public class GitVersionCommandExecutor
    {
        public GitVersionCommandExecutor(ILog log)
        {
            Log = log;            
        }

        public async Task<int> Execute(GlobalCommandOptions globalOptions, Func<Task<int>> execute)
        {
            if (globalOptions.LoggingMethod == LoggingMethod.Console)
            {
                Log.AddLogAppender(new ConsoleAppender());
            }
            else if (globalOptions.LoggingMethod == LoggingMethod.File && globalOptions.LogFilePath != "console")
            {
                Log.AddLogAppender(new FileAppender(globalOptions.LogFilePath));
            }

            try
            {
                //gitVersionTool.OutputVariables(variables);
                //gitVersionTool.UpdateAssemblyInfo(variables);
                //gitVersionTool.UpdateWixVersionFile(variables);
                var result = await execute.Invoke();
                return result;
            }
            catch (WarningException exception)
            {
                var error = $"An error occurred:{System.Environment.NewLine}{exception.Message}";
                Log.Warning(error);
                return 1;
            }
            catch (Exception exception)
            {
                var error = $"An unexpected error occurred:{System.Environment.NewLine}{exception}";
                Log.Error(error);

                Log.Info("Attempting to show the current git graph (please include in issue): ");
                Log.Info("Showing max of 100 commits");

                try
                {
                    // I am not sure here if we should be passing in working directory, or the dot git directory?
                    // current behaviour was to pass in working directory (environment current directory) so sticking with that.
                    LibGitExtensions.DumpGraph(globalOptions.WorkingDirectory, mess => Log.Info(mess), 100);
                }
                catch (Exception dumpGraphException)
                {
                    Log.Error("Couldn't dump the git graph due to the following error: " + dumpGraphException);
                }
                return 1;               
            }

            
        }

        public ILog Log { get; set; }
    }
}
