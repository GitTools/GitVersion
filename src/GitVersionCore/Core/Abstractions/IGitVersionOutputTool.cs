using GitVersion.OutputVariables;

namespace GitVersion
{
    public interface IGitVersionOutputTool
    {
        void OutputVariables(VersionVariables variables);
        void UpdateAssemblyInfo(VersionVariables variables);
        void UpdateWixVersionFile(VersionVariables variables);
        void GenerateGitVersionInformation(VersionVariables variables, FileWriteInfo fileWriteInfo);
    }
}
