namespace GitVersion
{
    using System;
    using System.Collections.Generic;

    public abstract class BuildServerBase : IBuildServer
    {
        public abstract bool CanApplyToCurrentContext();
        public abstract void PerformPreProcessingSteps(string gitDirectory);
        public abstract string GenerateSetVersionMessage(string versionToUseForBuildNumber);
        public abstract string[] GenerateSetParameterMessage(string name, string value);

        public virtual void WriteIntegration(SemanticVersion semanticVersion, Action<string> writer, Dictionary<string, string> variables)
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
            // TODO This should come from variable provider
            writer(GenerateSetVersionMessage(semanticVersion.ToString("f")));
            writer(string.Format("Executing GenerateBuildLogOutput for '{0}'.", GetType().Name));
            foreach (var buildParameter in BuildOutputFormatter.GenerateBuildLogOutput(this, variables))
            {
                writer(buildParameter);
            }
        }
    }
}