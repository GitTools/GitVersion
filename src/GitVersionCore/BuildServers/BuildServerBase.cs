using System;
using GitVersion.OutputFormatters;
using GitVersion.OutputVariables;

namespace GitVersion.BuildServers
{
    public abstract class BuildServerBase : IBuildServer
    {
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
