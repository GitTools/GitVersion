using GitVersion.MSBuildTask.Tasks;

namespace GitVersion.MSBuildTask
{
    public interface IGitVersionTaskExecutor
    {
        void GetVersion(GetVersion task);
        void UpdateAssemblyInfo(UpdateAssemblyInfo task);
        void GenerateGitVersionInformation(GenerateGitVersionInformation task);
        void WriteVersionInfoToBuildLog(WriteVersionInfoToBuildLog task);
    }
}
