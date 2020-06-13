using GitVersion.OutputVariables;

namespace GitVersion
{
    public interface IGitVersionTool
    {
        VersionVariables CalculateVersionVariables();
        void OutputVariables(VersionVariables variables);
        void UpdateAssemblyInfo(VersionVariables variables);
        void UpdateWixVersionFile(VersionVariables variables);
        void GenerateGitVersionInformation(VersionVariables variables, FileWriteInfo fileWriteInfo);
    }
}
