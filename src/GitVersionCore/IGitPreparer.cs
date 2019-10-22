using System;
using LibGit2Sharp;

namespace GitVersion
{
    public interface IGitPreparer
    {
        void Initialize(bool normalizeGitDirectory, string currentBranch, bool shouldCleanUpRemotes = false);
        TResult WithRepository<TResult>(Func<IRepository, TResult> action);
        string GetDotGitDirectory();
        string GetProjectRootDirectory();
        string TargetUrl { get; }
        string WorkingDirectory { get; }
    }
}