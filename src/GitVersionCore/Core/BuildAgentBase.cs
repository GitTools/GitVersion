using System;
using System.Collections.Generic;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion
{
    public abstract class BuildAgentBase : ICurrentBuildAgent
    {
        protected readonly ILog Log;
        protected IEnvironment Environment { get; }

        protected BuildAgentBase(IEnvironment environment, ILog log)
        {
            Log = log;
            Environment = environment;
        }

        protected abstract string EnvironmentVariable { get; }

        public abstract string GenerateSetVersionMessage(VersionVariables variables);
        public abstract string[] GenerateSetParameterMessage(string name, string value);

        public virtual bool CanApplyToCurrentContext() => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvironmentVariable));

        public virtual string GetCurrentBranch(bool usingDynamicRepos) => null;

        public virtual bool PreventFetch() => true;
        public virtual bool ShouldCleanUpRemotes() => false;

        public virtual void WriteIntegration(Action<string> writer, VersionVariables variables)
        {
            if (writer == null)
            {
                return;
            }

            writer($"Executing GenerateSetVersionMessage for '{GetType().Name}'.");
            writer(GenerateSetVersionMessage(variables));
            writer($"Executing GenerateBuildLogOutput for '{GetType().Name}'.");
            foreach (var buildParameter in GenerateBuildLogOutput(variables))
            {
                writer(buildParameter);
            }
        }

        protected IEnumerable<string> GenerateBuildLogOutput(VersionVariables variables)
        {
            var output = new List<string>();

            foreach (var variable in variables)
            {
                output.AddRange(GenerateSetParameterMessage(variable.Key, variable.Value));
            }

            return output;
        }
    }
}
