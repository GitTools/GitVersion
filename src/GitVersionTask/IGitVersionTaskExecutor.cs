using GitVersionTask.MsBuild.Tasks;

namespace GitVersionTask
{
    public interface IGitVersionTaskExecutor
    {
        void GetVersion(GetVersion task);
        void UpdateAssemblyInfo(UpdateAssemblyInfo task);
        void GenerateGitVersionInformation(GenerateGitVersionInformation task);
        void WriteVersionInfoToBuildLog(WriteVersionInfoToBuildLog task);
    }
}
