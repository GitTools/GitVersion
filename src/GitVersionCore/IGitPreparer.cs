namespace GitVersion
{
    public interface IGitPreparer
    {
        void Prepare(bool normalizeGitDirectory, string currentBranch, bool shouldCleanUpRemotes = false);
        string GetProjectRootDirectory();
        string GetDotGitDirectory();
        string GetTargetUrl();
        string GetWorkingDirectory();
    }
}
