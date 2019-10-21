using System;
using System.IO;
using GitVersion;
using GitVersion.Configuration;
using GitVersion.Exceptions;
using GitVersion.OutputFormatters;
using GitVersion.OutputVariables;
using GitVersionTask.MsBuild;
using GitVersionTask.MsBuild.Tasks;
using GitVersion.Extensions.GitVersionInformationResources;
using GitVersion.Extensions.VersionAssemblyInfoResources;
using GitVersion.Common;
using GitVersion.Logging;

namespace GitVersionTask
{
    public static class GitVersionTasks
    {
        private static readonly ILog log;
        private static readonly IFileSystem fileSystem;
        private static readonly IBuildServerResolver buildServerResolver;

        static GitVersionTasks()
        {
            log = new Log();
            fileSystem = new FileSystem();
            buildServerResolver = new BuildServerResolver(null, log);
        }

        public static bool GetVersion(GetVersion task)
        {
            return ExecuteGitVersionTask(task, t =>
            {
                if (!GetVersionVariables(t, out var versionVariables)) return;

                var outputType = typeof(GetVersion);
                foreach (var variable in versionVariables)
                {
                    outputType.GetProperty(variable.Key)?.SetValue(task, variable.Value, null);
                }
            });
        }

        public static bool UpdateAssemblyInfo(UpdateAssemblyInfo task)
        {
            return ExecuteGitVersionTask(task, t =>
            {
                FileHelper.DeleteTempFiles();
                FileHelper.CheckForInvalidFiles(t.CompileFiles, t.ProjectFile);

                if (!GetVersionVariables(t, out var versionVariables)) return;

                var fileWriteInfo = t.IntermediateOutputPath.GetFileWriteInfo(t.Language, t.ProjectFile, "AssemblyInfo");

                t.AssemblyInfoTempFilePath = Path.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);

                using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(fileWriteInfo.FileName, fileWriteInfo.WorkingDirectory, versionVariables, new FileSystem(), log, true);
                assemblyInfoFileUpdater.Update();
                assemblyInfoFileUpdater.CommitChanges();
            });
        }

        public static bool GenerateGitVersionInformation(GenerateGitVersionInformation task)
        {
            return ExecuteGitVersionTask(task, t =>
            {
                if (!GetVersionVariables(t, out var versionVariables)) return;

                var fileWriteInfo = t.IntermediateOutputPath.GetFileWriteInfo(t.Language, t.ProjectFile, "GitVersionInformation");

                t.GitVersionInformationFilePath = Path.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);
                var generator = new GitVersionInformationGenerator(fileWriteInfo.FileName, fileWriteInfo.WorkingDirectory, versionVariables, new FileSystem());
                generator.Generate();
            });
        }

        public static bool WriteVersionInfoToBuildLog(WriteVersionInfoToBuildLog task)
        {
            return ExecuteGitVersionTask(task, t =>
            {
                if (!GetVersionVariables(task, out var versionVariables)) return;

                var logger = t.Log;

                var buildServer = buildServerResolver.GetCurrentBuildServer();
                if (buildServer != null)
                {
                    logger.LogMessage($"Executing GenerateSetVersionMessage for '{ buildServer.GetType().Name }'.");
                    logger.LogMessage(buildServer.GenerateSetVersionMessage(versionVariables));
                    logger.LogMessage($"Executing GenerateBuildLogOutput for '{ buildServer.GetType().Name }'.");
                    foreach (var buildParameter in BuildOutputFormatter.GenerateBuildLogOutput(buildServer, versionVariables))
                    {
                        logger.LogMessage(buildParameter);
                    }
                }
            });
        }

        private static bool ExecuteGitVersionTask<T>(T task, Action<T> action)
            where T : GitVersionTaskBase
        {
            var taskLog = task.Log;
            try
            {
                action(task);
            }
            catch (WarningException errorException)
            {
                taskLog.LogWarningFromException(errorException);
                return true;
            }
            catch (Exception exception)
            {
                taskLog.LogErrorFromException(exception);
                return false;
            }

            return !taskLog.HasLoggedErrors;
        }

        private static bool GetVersionVariables(GitVersionTaskBase task, out VersionVariables versionVariables)
            => new ExecuteCore(fileSystem, log, GetConfigFileLocator(task.ConfigFilePath), buildServerResolver)
                .TryGetVersion(task.SolutionDirectory, out versionVariables, task.NoFetch, new Authentication());

        private static IConfigFileLocator GetConfigFileLocator(string filePath = null) =>
            !string.IsNullOrEmpty(filePath)
                ? (IConfigFileLocator) new NamedConfigFileLocator(filePath, fileSystem, log)
                : new DefaultConfigFileLocator(fileSystem, log);
    }
}
