namespace GitVersion
{
    using System;

    public abstract class BuildServerBase : IBuildServer
    {
        public abstract bool CanApplyToCurrentContext();
        public abstract void PerformPreProcessingSteps(string gitDirectory);
        public abstract string GenerateSetVersionMessage(string versionToUseForBuildNumber);
        public abstract string[] GenerateSetParameterMessage(string name, string value);

        public virtual void WriteIntegration(VersionAndBranch versionAndBranch, Action<string> writer)
        {
            if (versionAndBranch == null)
            {
                return;
            }

            if (writer == null)
            {
                return;
            }

            writer(string.Format("Executing GenerateSetVersionMessage for '{0}'.", GetType().Name));
            writer(GenerateSetVersionMessage(versionAndBranch.GenerateSemVer()));
            writer(string.Format("Executing GenerateBuildLogOutput for '{0}'.", GetType().Name));
            foreach (var buildParameter in BuildOutputFormatter.GenerateBuildLogOutput(versionAndBranch, this))
            {
                writer(buildParameter);
            }
        }
    }
}