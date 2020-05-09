using GitVersion.OutputVariables;

namespace GitVersion
{


    public interface IGitVersionTool : IGitVersionCalculator
    {
        void OutputVariables(VersionVariables variables);
        void UpdateAssemblyInfo(VersionVariables variables);
        void UpdateWixVersionFile(VersionVariables variables);
        void GenerateGitVersionInformation(VersionVariables variables, FileWriteInfo fileWriteInfo);
    }
}
