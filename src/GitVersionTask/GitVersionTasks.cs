namespace GitVersionTask
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using GitVersion;
    using GitVersion.Helpers;
    using Microsoft.Build.Framework;

    public static class GitVersionTasks
    {
        public static bool GetVersion(GetVersion task)
        {
            return ExecuteGitVersionTask(task, t =>
            {
                if (!GitVersionTaskUtils.GetVersionVariables(t, out var versionVariables)) return;

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

                if (!GitVersionTaskUtils.GetVersionVariables(t, out var versionVariables)) return;

                CreateTempAssemblyInfo(t, versionVariables);
            });
        }

        public static bool WriteVersionInfoToBuildLog(WriteVersionInfoToBuildLog task)
        {
            return ExecuteGitVersionTask(task, t =>
            {
                if (!GitVersionTaskUtils.GetVersionVariables(task, out var versionVariables)) return;

                WriteIntegrationParameters(t, BuildServerList.GetApplicableBuildServers(), versionVariables);
            });
        }

        public static bool GenerateGitVersionInformation(GenerateGitVersionInformation task)
        {
            return ExecuteGitVersionTask(task, t =>
            {
                if (!GitVersionTaskUtils.GetVersionVariables(t, out var versionVariables)) return;

                CreateGitVersionInfo(t, versionVariables);
            });
        }

        private static void CreateTempAssemblyInfo(UpdateAssemblyInfo task, VersionVariables versionVariables)
        {
            var fileWriteInfo = task.IntermediateOutputPath.GetFileWriteInfo(
                task.Language,
                task.ProjectFile,
                (pf, ext) => $"AssemblyInfo.g.{ext}",
                (pf, ext) => $"AssemblyInfo_{Path.GetFileNameWithoutExtension(pf)}_{Path.GetRandomFileName()}.g.{ext}"
            );

            task.AssemblyInfoTempFilePath = Path.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);

            using (var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(fileWriteInfo.FileName, fileWriteInfo.WorkingDirectory, versionVariables, new FileSystem(), true))
            {
                assemblyInfoFileUpdater.Update();
                assemblyInfoFileUpdater.CommitChanges();
            }
        }

        private static void WriteIntegrationParameters(WriteVersionInfoToBuildLog task, IEnumerable<IBuildServer> applicableBuildServers, VersionVariables versionVariables)
        {
            var logger = task.Log;
            foreach (var buildServer in applicableBuildServers)
            {
                logger.LogMessage($"Executing GenerateSetVersionMessage for '{ buildServer.GetType().Name }'.");
                logger.LogMessage(buildServer.GenerateSetVersionMessage(versionVariables));
                logger.LogMessage($"Executing GenerateBuildLogOutput for '{ buildServer.GetType().Name }'.");
                foreach (var buildParameter in BuildOutputFormatter.GenerateBuildLogOutput(buildServer, versionVariables))
                {
                    logger.LogMessage(buildParameter);
                }
            }
        }

        private static void CreateGitVersionInfo(GenerateGitVersionInformation task, VersionVariables versionVariables)
        {
            var fileWriteInfo = task.IntermediateOutputPath.GetFileWriteInfo(
                task.Language,
                task.ProjectFile,
                (pf, ext) => $"GitVersionInformation.g.{ext}",
                (pf, ext) => $"GitVersionInformation_{Path.GetFileNameWithoutExtension(pf)}_{Path.GetRandomFileName()}.g.{ext}"
            );

            task.GitVersionInformationFilePath = Path.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);
            var generator = new GitVersionInformationGenerator(fileWriteInfo.FileName, fileWriteInfo.WorkingDirectory, versionVariables, new FileSystem());
            generator.Generate();
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
    }
}
