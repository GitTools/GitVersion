using System;
using System.Collections.Generic;
using System.Linq;
using GitTools.Testing;
using GitVersion.BuildAgents;
using GitVersionTask.Tests.Mocks;
using Microsoft.Build.Framework;

namespace GitVersionTask.Tests.Helpers
{
    public class MsBuildTaskFixture
    {
        private readonly RepositoryFixtureBase fixture;
        private KeyValuePair<string, string>[] environmentVariables;

        public MsBuildTaskFixture(RepositoryFixtureBase fixture)
        {
            this.fixture = fixture;
        }

        public void WithEnv(params KeyValuePair<string, string>[] envs)
        {
            environmentVariables = envs;
        }

        public MsBuildTaskFixtureResult<T> Execute<T>(T task) where T : ITask
        {
            return UsingEnv(() =>
            {
                var buildEngine = new MockEngine();

                task.BuildEngine = buildEngine;

                var result = task.Execute();

                return new MsBuildTaskFixtureResult<T>(fixture)
                {
                    Success = result,
                    Task = task,
                    Errors = buildEngine.Errors,
                    Warnings = buildEngine.Warnings,
                    Messages = buildEngine.Messages,
                    Log = buildEngine.Log,
                };
            });
        }

        private T UsingEnv<T>(Func<T> func)
        {
            ResetEnvironment();
            SetEnvironmentVariables(environmentVariables);

            try
            {
                return func();
            }
            finally
            {
                ResetEnvironment();
            }
        }

        private static void ResetEnvironment()
        {
            var environmentalVariables = new Dictionary<string, string>
            {
                { TeamCity.EnvironmentVariableName, null },
                { AppVeyor.EnvironmentVariableName, null },
                { TravisCi.EnvironmentVariableName, null },
                { Jenkins.EnvironmentVariableName, null },
                { AzurePipelines.EnvironmentVariableName, null },
                { GitHubActions.EnvironmentVariableName, null },
            };

            SetEnvironmentVariables(environmentalVariables.ToArray());
        }

        private static void SetEnvironmentVariables(KeyValuePair<string, string>[] envs)
        {
            if (envs == null) return;
            foreach (var env in envs)
            {
                Environment.SetEnvironmentVariable(env.Key, env.Value);
            }
        }
    }
}
