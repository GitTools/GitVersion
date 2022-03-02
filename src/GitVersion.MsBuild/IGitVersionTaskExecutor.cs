using GitVersion.MsBuild.Tasks;

namespace GitVersion.MsBuild;

public interface IGitVersionTaskExecutor
{
    void GetVersion(GetVersion task);
    void UpdateAssemblyInfo(UpdateAssemblyInfo task);
    void GenerateGitVersionInformation(GenerateGitVersionInformation task);
    void WriteVersionInfoToBuildLog(WriteVersionInfoToBuildLog task);
}
