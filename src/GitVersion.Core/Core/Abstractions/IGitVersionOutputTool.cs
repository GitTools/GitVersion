using GitVersion.OutputVariables;

namespace GitVersion;

public interface IGitVersionOutputTool
{
    void OutputVariables(VersionVariables variables, bool updateBuildNumber);
    void UpdateAssemblyInfo(VersionVariables variables);
    void UpdateWixVersionFile(VersionVariables variables);
    void GenerateGitVersionInformation(VersionVariables variables, FileWriteInfo fileWriteInfo);
}
