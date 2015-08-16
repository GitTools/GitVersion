namespace GitVersion
{
    using System;

    public interface IBuildServer
    {
        bool CanApplyToCurrentContext();
        string GenerateSetVersionMessage(string versionToUseForBuildNumber);
        string[] GenerateSetParameterMessage(string name, string value);

        void WriteIntegration(Action<string> writer, VersionVariables variables);
        string GetCurrentBranch();
    }
}
