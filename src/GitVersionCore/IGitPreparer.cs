namespace GitVersion
{
    public interface IGitPreparer
    {
        string GetProjectRootDirectory();
        string GetDotGitDirectory();
        string GetTargetUrl();
        string GetWorkingDirectory();
        void Prepare();
    }
}
