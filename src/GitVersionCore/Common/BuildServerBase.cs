using System;
using GitVersion.Logging;
using GitVersion.OutputFormatters;
using GitVersion.OutputVariables;

namespace GitVersion
{
    using System.Linq;

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

            var keys = string.Join(";", variables.Where(pair => !string.IsNullOrWhiteSpace(pair.Value)).Select(pair => pair.Key));

            writer($"Executing GenerateSetVersionMessage for '{GetType().Name}'.");
            writer(GenerateSetVersionMessage(variables));
            writer($"Executing GenerateBuildLogOutput for '{GetType().Name}' Keys:{keys}.");
            foreach (var buildParameter in BuildOutputFormatter.GenerateBuildLogOutput(this, variables))
            {
                writer(buildParameter);
            }
        }

        public virtual bool ShouldCleanUpRemotes()
        {
            return false;
        }
    }
}
