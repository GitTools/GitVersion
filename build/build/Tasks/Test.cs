using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Build.Utils;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNetCore.Test;
using Cake.Core.IO;
using Cake.Coverlet;
using Cake.Frosting;

namespace Build.Tasks
{
    [TaskName(nameof(Test))]
    [TaskDescription("Run the unit tests")]
    [IsDependentOn(typeof(Build))]
    public class Test : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context) => context.EnabledUnitTests;

        public override void Run(BuildContext context)
        {
            var dotnetTarget = context.Argument(Arguments.DotnetTarget, string.Empty);
            var frameworks = new[] { Constants.CoreFxVersion31, Constants.FullFxVersion48, Constants.NetVersion50 };
            if (!string.IsNullOrWhiteSpace(dotnetTarget))
            {
                if (!frameworks.Contains(dotnetTarget, StringComparer.OrdinalIgnoreCase))
                {
                    throw new Exception($"Dotnet Target {dotnetTarget} is not supported at the moment");
                }
                frameworks = new[] { dotnetTarget };
            }

            foreach (var framework in frameworks)
            {
                // run using dotnet test
                var actions = new List<Action>();
                var projects = context.GetFiles("./src/**/*.Tests.csproj");
                foreach (var project in projects)
                {
                    actions.Add(() =>
                    {
                        TestProjectForTarget(context, project, framework);
                    });
                }

                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = -1,
                    CancellationToken = default
                };

                Parallel.Invoke(options, actions.ToArray());
            }
        }
        private static void TestProjectForTarget(BuildContext context, FilePath project, string framework)
        {
            var testResultsPath = Paths.TestOutput;
            var projectName = $"{project.GetFilenameWithoutExtension()}.{framework}";
            var settings = new DotNetCoreTestSettings
            {
                Framework = framework,
                NoBuild = true,
                NoRestore = true,
                Configuration = context.MsBuildConfiguration,
            };

            if (!context.IsRunningOnMacOs())
            {
                settings.TestAdapterPath = new DirectoryPath(".");
                var resultsPath = context.MakeAbsolute(DirectoryPath.FromString(testResultsPath).CombineWithFilePath($"{projectName}.results.xml"));
                settings.Loggers = new[] { $"nunit;LogFilePath={resultsPath}" };
            }

            var coverletSettings = new CoverletSettings
            {
                CollectCoverage = true,
                CoverletOutputFormat = CoverletOutputFormat.cobertura,
                CoverletOutputDirectory = testResultsPath,
                CoverletOutputName = $"{projectName}.coverage.xml",
                Exclude = new List<string> { "[GitVersion*.Tests]*", "[GitTools.Testing]*" }
            };

            if (string.Equals(framework, Constants.FullFxVersion48))
            {
                settings.Filter = context.IsRunningOnUnix() ? "TestCategory!=NoMono" : "TestCategory!=NoNet48";
            }

            context.DotNetCoreTest(project.FullPath, settings, coverletSettings);
        }
    }
}
