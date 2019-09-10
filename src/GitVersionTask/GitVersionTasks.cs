using System;
using System.IO;
using GitVersion;
using GitVersion.BuildServers;
using GitVersion.Configuration;
using GitVersion.Exceptions;
using GitVersion.Helpers;
using GitVersion.OutputFormatters;
using GitVersion.OutputVariables;
using GitVersionTask.MsBuild;
using GitVersionTask.MsBuild.Tasks;
using Microsoft.Build.Framework;
using GitVersion.Extensions.GitVersionInformationResources;
using GitVersion.Extensions.VersionAssemblyInfoResources;
using GitVersion.Common;
using Environment = GitVersion.Common.Environment;

namespace GitVersionTask
{
    public static class GitVersionTasks
    {
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

                using (var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(fileWriteInfo.FileName, fileWriteInfo.WorkingDirectory, versionVariables, new FileSystem(), true))
                {
                    assemblyInfoFileUpdater.Update();
                    assemblyInfoFileUpdater.CommitChanges();
                }
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
                BuildServerList.Init(new Environment());
                foreach (var buildServer in BuildServerList.GetApplicableBuildServers())
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
            void LogDebug(string message) => task.Log.LogMessage(MessageImportance.Low, message);
            void LogInfo(string message) => task.Log.LogMessage(MessageImportance.Normal, message);
            void LogWarning(string message) => task.Log.LogWarning(message);
            void LogError(string message) => task.Log.LogError(message);

            Logger.SetLoggers(LogDebug, LogInfo, LogWarning, LogError);
            var log = task.Log;
            try
            {
                action(task);
            }
            catch (WarningException errorException)
            {
                log.LogWarningFromException(errorException);
                return true;
            }
            catch (Exception exception)
            {
                log.LogErrorFromException(exception);
                return false;
            }

            return !log.HasLoggedErrors;
        }

        private static bool GetVersionVariables(GitVersionTaskBase task, out VersionVariables versionVariables)
            => new ExecuteCore(new FileSystem(), new Environment(), ConfigFileLocator.GetLocator(task.ConfigFilePath)).TryGetVersion(task.SolutionDirectory, out versionVariables, task.NoFetch, new Authentication());
    }
}
