namespace GitVersion
{
    using System;
    using System.Collections.Generic;

    public interface IBuildServer
    {
        bool CanApplyToCurrentContext();
        void PerformPreProcessingSteps(string gitDirectory);
        string GenerateSetVersionMessage(string versionToUseForBuildNumber);
        string[] GenerateSetParameterMessage(string name, string value);

        void WriteIntegration(SemanticVersion semanticVersion, Action<string> writer, Dictionary<string,string> variables);
    }

}
