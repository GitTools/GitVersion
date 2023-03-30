using GitVersion.MsBuild.Tasks;

namespace GitVersion.MsBuild;

internal interface IGitVersionTaskExecutor
{
    void GetVersion(GetVersion task);
    void UpdateAssemblyInfo(UpdateAssemblyInfo task);
    void GenerateGitVersionInformation(GenerateGitVersionInformation task);
    void WriteVersionInfoToBuildLog(WriteVersionInfoToBuildLog task);
}
