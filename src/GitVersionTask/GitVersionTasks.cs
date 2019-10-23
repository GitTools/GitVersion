using System;
using System.IO;
using GitVersion;
using GitVersion.Exceptions;
using GitVersion.Extensions;
using GitVersion.OutputFormatters;
using GitVersion.OutputVariables;
using GitVersion.Extensions.GitVersionInformationResources;
using GitVersion.Extensions.VersionAssemblyInfoResources;
using GitVersion.Logging;
using GitVersionTask.MsBuild;
using GitVersionTask.MsBuild.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitVersionTask
{
    public static class GitVersionTasks
    {
        public static bool GetVersion(GetVersion task)
        {
            return ExecuteGitVersionTask(task, (t, sp) =>
            {
                if (!GetVersionVariables(sp, out var versionVariables)) return;

                var outputType = typeof(GetVersion);
                foreach (var variable in versionVariables)
                {
                    outputType.GetProperty(variable.Key)?.SetValue(t, variable.Value, null);
                }
            });
        }

        public static bool UpdateAssemblyInfo(UpdateAssemblyInfo task)
        {
            return ExecuteGitVersionTask(task, (t, sp) =>
            {
                var log = sp.GetService<ILog>();
                FileHelper.DeleteTempFiles();
                FileHelper.CheckForInvalidFiles(t.CompileFiles, t.ProjectFile);

                if (!GetVersionVariables(sp, out var versionVariables)) return;

                var fileWriteInfo = t.IntermediateOutputPath.GetFileWriteInfo(t.Language, t.ProjectFile, "AssemblyInfo");

                t.AssemblyInfoTempFilePath = Path.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);

                using var assemblyInfoFileUpdater = new AssemblyInfoFileUpdater(fileWriteInfo.FileName, fileWriteInfo.WorkingDirectory, versionVariables, new FileSystem(), log, true);
                assemblyInfoFileUpdater.Update();
                assemblyInfoFileUpdater.CommitChanges();
            });
        }

        public static bool GenerateGitVersionInformation(GenerateGitVersionInformation task)
        {
            return ExecuteGitVersionTask(task, (t, sp) =>
            {
                if (!GetVersionVariables(sp, out var versionVariables)) return;

                var fileWriteInfo = t.IntermediateOutputPath.GetFileWriteInfo(t.Language, t.ProjectFile, "GitVersionInformation");

                t.GitVersionInformationFilePath = Path.Combine(fileWriteInfo.WorkingDirectory, fileWriteInfo.FileName);
                var generator = new GitVersionInformationGenerator(fileWriteInfo.FileName, fileWriteInfo.WorkingDirectory, versionVariables, new FileSystem());
                generator.Generate();
            });
        }

        public static bool WriteVersionInfoToBuildLog(WriteVersionInfoToBuildLog task)
        {
            return ExecuteGitVersionTask(task, (t, sp) =>
            {
                if (!GetVersionVariables(sp, out var versionVariables)) return;

                var logger = t.Log;

                var buildServerResolver = sp.GetService<IBuildServerResolver>();
                var buildServer = buildServerResolver.Resolve();
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

        private static bool ExecuteGitVersionTask<T>(T task, Action<T, IServiceProvider> action)
            where T : GitVersionTaskBase
        {
            var taskLog = task.Log;
            try
            {
                var sp = BuildServiceProvider(task);
                action(task, sp);
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

        private static IServiceProvider BuildServiceProvider(GitVersionTaskBase task)
        {
            var services = new ServiceCollection();

            var arguments = new Arguments
            {
                TargetPath = task.SolutionDirectory,
                ConfigFile = task.ConfigFilePath,
                NoFetch = task.NoFetch
            };

            services.AddSingleton(_ => Options.Create(arguments));
            services.AddModule(new GitVersionCoreModule());

            var sp = services.BuildServiceProvider();
            return sp;
        }

        private static bool GetVersionVariables(IServiceProvider sp, out VersionVariables versionVariables)
        {
            var arguments = sp.GetService<IOptions<Arguments>>().Value;
            var gitVersionCalculator = sp.GetService<IGitVersionCalculator>();
            return gitVersionCalculator.TryCalculateVersionVariables(arguments, out versionVariables);
        }
    }
}
