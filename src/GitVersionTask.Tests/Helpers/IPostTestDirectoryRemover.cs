namespace GitVersion.MSBuildTask.Tests.Helpers
{
    public interface IPostTestDirectoryRemover
    {
        void Register(string directoryPath);
    }
}