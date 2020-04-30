using System;
using System.Collections.Generic;
using System.IO;
using Buildalyzer;
using Buildalyzer.Environment;
using GitTools.Testing;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Build.Utilities.ProjectCreation;
using NUnit.Framework.Internal;
using StringWriter = System.IO.StringWriter;

namespace GitVersionTask.Tests.Helpers
{
    public class MsBuildExeFixture
    {
        private readonly RepositoryFixtureBase fixture;
        private KeyValuePair<string, string>[] environmentVariables;

        public void WithEnv(params KeyValuePair<string, string>[] envs)
        {
            environmentVariables = envs;
        }

        public const string OutputTarget = "GitVersionOutput";

        private readonly AnalyzerManager manager = new AnalyzerManager();
        private readonly string ProjectPath;

        public MsBuildExeFixture(RepositoryFixtureBase fixture, string workingDirectory = "")
        {
            this.fixture = fixture;
            ProjectPath = Path.Combine(workingDirectory, "app.csproj");
        }

        public MsBuildExeFixtureResult Execute()
        {
            var analyzer = manager.GetProject(ProjectPath);

            var output = new StringWriter();
            analyzer.AddBuildLogger(new ConsoleLogger(LoggerVerbosity.Normal, output.Write, null, null));

            var environmentOptions = new EnvironmentOptions { DesignTime = false };
            environmentOptions.TargetsToBuild.Clear();
            environmentOptions.TargetsToBuild.Add(OutputTarget);

            if (environmentVariables != null)
            {
                foreach (var pair in environmentVariables)
                {
                    analyzer.SetEnvironmentVariable(pair.Key, pair.Value);
                }
            }

            var results = analyzer.Build(environmentOptions);

            return new MsBuildExeFixtureResult(fixture)
            {
                ProjectPath = ProjectPath,
                Output = output.ToString(),
                MsBuild = results
            };
        }

        public void CreateTestProject(Action<ProjectCreator> extendProject)
        {
            var project = RuntimeFramework.CurrentFramework.Runtime switch
            {
                RuntimeType.NetCore => ProjectCreator.Templates.SdkCsproj(ProjectPath),
                RuntimeType.Net => ProjectCreator.Templates.LegacyCsproj(ProjectPath, defaultTargets: null, targetFrameworkVersion: "v4.7.2", toolsVersion: "15.0"),
                _ => null
            };

            if (project == null) return;

            extendProject(project);
            project.Save();
        }
    }
}
