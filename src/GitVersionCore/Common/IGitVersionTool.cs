using System;
using GitVersion.Extensions.GitVersionInformationResources;
using GitVersion.OutputVariables;

namespace GitVersion
{
    public interface IGitVersionTool
    {
        VersionVariables CalculateVersionVariables();
        void OutputVariables(VersionVariables variables, Action<string> writter);
        void UpdateAssemblyInfo(VersionVariables variables);
        void UpdateWixVersionFile(VersionVariables variables);
        void GenerateGitVersionInformation(VersionVariables variables, FileWriteInfo fileWriteInfo);
    }
}
