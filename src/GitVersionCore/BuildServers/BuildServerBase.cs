namespace GitVersion
{
    using System;

    public abstract class BuildServerBase : IBuildServer
    {
        public abstract bool CanApplyToCurrentContext();
        public abstract string GenerateSetVersionMessage(string versionToUseForBuildNumber);
        public abstract string[] GenerateSetParameterMessage(string name, string value);
        public abstract string GetCurrentBranch();

        public virtual void WriteIntegration(Action<string> writer, VersionVariables variables)
        {
            if (writer == null)
            {
                return;
            }

            writer(string.Format("Executing GenerateSetVersionMessage for '{0}'.", GetType().Name));
            writer(GenerateSetVersionMessage(variables.FullSemVer));
            writer(string.Format("Executing GenerateBuildLogOutput for '{0}'.", GetType().Name));
            foreach (var buildParameter in BuildOutputFormatter.GenerateBuildLogOutput(this, variables))
            {
                writer(buildParameter);
            }
        }
    }
}