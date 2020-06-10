using System.Collections.Generic;
using LibGit2Sharp;

namespace GitVersion
{
    /// <summary>
    /// Mockable and testable interface wrapper for the <c>static</c>
    /// <see cref="Commands"/> <c>class</c>.
    /// </summary>
    public interface IGitRepositoryCommands
    {
        Branch Checkout(string committishOrBranchSpec);
        Branch Checkout(string committishOrBranchSpec, CheckoutOptions options);
        Branch Checkout(Branch branch);
        Branch Checkout(Branch branch, CheckoutOptions options);
        Branch Checkout(Commit commit);
        Branch Checkout(Commit commit, CheckoutOptions options);
        void Checkout(Tree tree, CheckoutOptions checkoutOptions, string refLogHeadSpec);
        void Fetch(string remote, IEnumerable<string> refspecs, FetchOptions options, string logMessage);
        void Move(string sourcePath, string destinationPath);
        void Move(IEnumerable<string> sourcePaths, IEnumerable<string> destinationPaths);
        MergeResult Pull(Signature merger, PullOptions options);
        void Remove(string path, bool removeFromWorkingDirectory);
        void Remove(IEnumerable<string> paths);
        void Remove(IEnumerable<string> paths, bool removeFromWorkingDirectory, ExplicitPathsOptions explicitPathsOptions);
        void Remove(string path);
        void Remove(string path, bool removeFromWorkingDirectory, ExplicitPathsOptions explicitPathsOptions);
        void Stage(string path);
        void Stage(string path, StageOptions stageOptions);
        void Stage(IEnumerable<string> paths);
        void Stage(IEnumerable<string> paths, StageOptions stageOptions);
        void Unstage(string path);
        void Unstage(string path, ExplicitPathsOptions explicitPathsOptions);
        void Unstage(IEnumerable<string> paths);
        void Unstage(IEnumerable<string> paths, ExplicitPathsOptions explicitPathsOptions);
    }
}
