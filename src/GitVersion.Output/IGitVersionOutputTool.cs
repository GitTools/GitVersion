using GitVersion.OutputVariables;

namespace GitVersion;

public interface IGitVersionOutputTool
{
    void OutputVariables(GitVersionVariables variables, bool updateBuildNumber);
    void UpdateAssemblyInfo(GitVersionVariables variables);
    void UpdateWixVersionFile(GitVersionVariables variables);
    void GenerateGitVersionInformation(GitVersionVariables variables, FileWriteInfo fileWriteInfo);
}
