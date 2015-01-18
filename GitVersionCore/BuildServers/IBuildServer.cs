﻿namespace GitVersion
{
    using System;

    public interface IBuildServer
    {
        bool CanApplyToCurrentContext();
        void PerformPreProcessingSteps(string gitDirectory);
        string GenerateSetVersionMessage(string versionToUseForBuildNumber);
        string[] GenerateSetParameterMessage(string name, string value);

        void WriteIntegration(Action<string> writer, VersionVariables variables);
    }

}
