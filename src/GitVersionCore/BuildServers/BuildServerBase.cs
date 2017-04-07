namespace GitVersion
{
    using System;

    public abstract class BuildServerBase : IBuildServer
    {
        public abstract bool CanApplyToCurrentContext();
        public abstract string GenerateSetVersionMessage(VersionVariables variables);
        public abstract string[] GenerateSetParameterMessage(string name, string value);

        public virtual string GetCurrentBranch(bool usingDynamicRepos)
        {
            return null;
        }

        public virtual bool PreventFetch()
        {
            return false;
        }

        public virtual void WriteIntegration(Action<string> writer, VersionVariables variables)
        {
            if (writer == null)
            {
                return;
            }

            writer(string.Format("Executing GenerateSetVersionMessage for '{0}'.", GetType().Name));
            writer(GenerateSetVersionMessage(variables));
            writer(string.Format("Executing GenerateBuildLogOutput for '{0}'.", GetType().Name));
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