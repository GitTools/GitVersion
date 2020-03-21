using System;
using System.Collections.Generic;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion
{
    public abstract class BuildServerBase : IBuildServer
    {
        protected readonly ILog Log;
        protected IEnvironment Environment { get; }

        protected BuildServerBase(IEnvironment environment, ILog log)
        {
            Log = log;
            Environment = environment;
        }

        protected abstract string EnvironmentVariable { get; }

        public virtual bool CanApplyToCurrentContext()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(EnvironmentVariable));
        }

        public abstract string GenerateSetVersionMessage(VersionVariables variables);
        public abstract string[] GenerateSetParameterMessage(string name, string value);

        public virtual string GetCurrentBranch(bool usingDynamicRepos)
        {
            return null;
        }

        public virtual bool PreventFetch() => true;

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

        public IEnumerable<string> GenerateBuildLogOutput(VersionVariables variables)
        {
            var output = new List<string>();

            foreach (var variable in variables)
            {
                output.AddRange(GenerateSetParameterMessage(variable.Key, variable.Value));
            }

            return output;
        }

        public virtual bool ShouldCleanUpRemotes()
        {
            return false;
        }
    }
}
