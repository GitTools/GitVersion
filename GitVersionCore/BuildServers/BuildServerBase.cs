namespace GitVersion
{
    using System;

    public abstract class BuildServerBase : IBuildServer
    {
        public abstract bool CanApplyToCurrentContext();
        public abstract void PerformPreProcessingSteps(string gitDirectory);
        public abstract string GenerateSetVersionMessage(string versionToUseForBuildNumber);
        public abstract string[] GenerateSetParameterMessage(string name, string value);

        public virtual void WriteIntegration(SemanticVersion semanticVersion, Action<string> writer)
        {
            if (semanticVersion == null)
            {
                return;
            }

            if (writer == null)
            {
                return;
            }

            writer(string.Format("Executing GenerateSetVersionMessage for '{0}'.", GetType().Name));
            writer(GenerateSetVersionMessage(semanticVersion.ToString("f")));
            writer(string.Format("Executing GenerateBuildLogOutput for '{0}'.", GetType().Name));
            foreach (var buildParameter in BuildOutputFormatter.GenerateBuildLogOutput(semanticVersion, this))
            {
                writer(buildParameter);
            }
        }
    }
}