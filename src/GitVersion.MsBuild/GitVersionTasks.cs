namespace GitVersion.MsBuild
{
    using System;
    using System.IO;
    using GitVersion;
    using GitVersion.Helpers;
    using GitVersion.MsBuild.Task;
    using Microsoft.Build.Framework;

    public static class GitVersionTasks
    {
        public static ExecuteCore ExecuteCore { get; }

        static GitVersionTasks()
        {
            var fileSystem = new FileSystem();
            ExecuteCore = new ExecuteCore(fileSystem);
        }

        public static bool GetVersion(GetVersion task)
        {
            return Execute(task, t =>
            {
                if (!GetVersionVariables(task, out var versionVariables)) return;

                var thisType = typeof(GetVersion);
                foreach (var variable in versionVariables)
                {
                    thisType.GetProperty(variable.Key)?.SetValue(task, variable.Value, null);
                }
            });
        }

        public static bool GenerateGitVersionInformation(GenerateGitVersionInformation task)
        {
            return Execute(task, t =>
            {
                if (!GetVersionVariables(task, out var versionVariables)) return;

                var fileExtension = GetFileExtension(task.Language);
                var fileName = $"GitVersionInformation.g.{fileExtension}";

                if (task.IntermediateOutputPath == null)
                {
                    fileName = $"GitVersionInformation_{Path.GetFileNameWithoutExtension(task.ProjectFile)}_{Path.GetRandomFileName()}.g.{fileExtension}";
                }

                var workingDirectory = task.IntermediateOutputPath ?? TempFileTracker.TempPath;

                task.GitVersionInformationFilePath = Path.Combine(workingDirectory, fileName);

                var generator = new GitVersionInformationGenerator(fileName, workingDirectory, versionVariables, new FileSystem());
                generator.Generate();
            });
        }

        public static bool UpdateAssemblyInfo(UpdateAssemblyInfo task)
        {
            return Execute(task, t =>
            {
                TempFileTracker.DeleteTempFiles();
                InvalidFileChecker.CheckForInvalidFiles(task.CompileFiles, task.ProjectFile);

                if (!GetVersionVariables(task, out var versionVariables)) return;

                var fileExtension = GetFileExtension(task.Language);
                var assemblyInfoFileName = $"GitVersionTaskAssemblyInfo.g.{fileExtension}";

                if (task.IntermediateOutputPath == null)
                {
                    assemblyInfoFileName = $"AssemblyInfo_{Path.GetFileNameWithoutExtension(task.ProjectFile)}_{Path.GetRandomFileName()}.g.{fileExtension}";
                }

                var workingDirectory = task.IntermediateOutputPath ?? TempFileTracker.TempPath;

                task.AssemblyInfoTempFilePath = Path.Combine(workingDirectory, assemblyInfoFileName);

                using (var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(assemblyInfoFileName, workingDirectory, versionVariables, new FileSystem(), true))
                {
                    assemblyInfoFileUpdater.Update();
                    assemblyInfoFileUpdater.CommitChanges();
                }
            });
        }

        public static bool WriteVersionInfoToBuildLog(WriteVersionInfoToBuildLog task)
        {
            return Execute(task, t =>
            {
                if (!GetVersionVariables(task, out var versionVariables)) return;
                var log = task.Log;

                foreach (var buildServer in BuildServerList.GetApplicableBuildServers())
                {
                    log.LogMessage($"Executing GenerateSetVersionMessage for '{buildServer.GetType().Name}'.");
                    log.LogMessage(buildServer.GenerateSetVersionMessage(versionVariables));
                    log.LogMessage($"Executing GenerateBuildLogOutput for '{buildServer.GetType().Name}'.");
                    foreach (var buildParameter in BuildOutputFormatter.GenerateBuildLogOutput(buildServer, versionVariables))
                    {
                        log.LogMessage(buildParameter);
                    }
                }
            });
        }

        private static bool Execute<T>(T task, Action<T> action)
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
            => ExecuteCore.TryGetVersion(task.SolutionDirectory, out versionVariables, task.NoFetch, new Authentication());

        private static string GetFileExtension(string language)
        {
            switch(language)
            {
                case "C#":
                    return "cs";

                case "F#":
                    return "fs";

                case "VB":
                    return "vb";

                default:
                    throw new Exception($"Unknown language detected: '{language}'");
            }
        }
    }
}
