using System.Linq;
using GitVersion.Extensions;
using GitVersion.Logging;
using LibGit2Sharp;

namespace GitVersion.Helpers
{
    public static class GitRepositoryHelper
    {
        /// <summary>
        /// Normalization of a git directory turns all remote branches into local branches, turns pull request refs into a real branch and a few other things. This is designed to be run *only on the build server* which checks out repositories in different ways.
        /// It is not recommended to run normalization against a local repository
        /// </summary>
        public static void NormalizeGitDirectory(ILog log, IEnvironment environment, string gitDirectory, AuthenticationInfo authentication,
            bool noFetch, string currentBranch, bool isDynamicRepository)
        {
            using var repository = new Repository(gitDirectory);
            // Need to ensure the HEAD does not move, this is essentially a BugCheck
            var expectedSha = repository.Head.Tip.Sha;
            var expectedBranchName = repository.Head.CanonicalName;

            try
            {
                var remote = repository.EnsureOnlyOneRemoteIsDefined(log);

                repository.AddMissingRefSpecs(log, remote);

                //If noFetch is enabled, then GitVersion will assume that the git repository is normalized before execution, so that fetching from remotes is not required.
                if (noFetch)
                {
                    log.Info("Skipping fetching, if GitVersion does not calculate your version as expected you might need to allow fetching or use dynamic repositories");
                }
                else
                {
                    log.Info($"Fetching from remote '{remote.Name}' using the following refspecs: {string.Join(", ", remote.FetchRefSpecs.Select(r => r.Specification))}.");
                    Commands.Fetch(repository, remote.Name, new string[0], authentication.ToFetchOptions(), null);
                }

                repository.EnsureLocalBranchExistsForCurrentBranch(log, remote, currentBranch);
                repository.CreateOrUpdateLocalBranchesFromRemoteTrackingOnes(log, remote.Name);

                // Bug fix for https://github.com/GitTools/GitVersion/issues/1754, head maybe have been changed
                // if this is a dynamic repository. But only allow this in case the branches are different (branch switch)
                if (expectedSha != repository.Head.Tip.Sha &&
                    (isDynamicRepository || !expectedBranchName.IsBranch(currentBranch)))
                {
                    var newExpectedSha = repository.Head.Tip.Sha;
                    var newExpectedBranchName = repository.Head.CanonicalName;

                    log.Info($"Head has moved from '{expectedBranchName} | {expectedSha}' => '{newExpectedBranchName} | {newExpectedSha}', allowed since this is a dynamic repository");

                    expectedSha = newExpectedSha;
                }

                var headSha = repository.Refs.Head.TargetIdentifier;

                if (!repository.Info.IsHeadDetached)
                {
                    log.Info($"HEAD points at branch '{headSha}'.");
                    return;
                }

                log.Info($"HEAD is detached and points at commit '{headSha}'.");
                log.Info($"Local Refs:{System.Environment.NewLine}" + string.Join(System.Environment.NewLine, repository.Refs.FromGlob("*").Select(r => $"{r.CanonicalName} ({r.TargetIdentifier})")));

                // In order to decide whether a fake branch is required or not, first check to see if any local branches have the same commit SHA of the head SHA.
                // If they do, go ahead and checkout that branch
                // If no, go ahead and check out a new branch, using the known commit SHA as the pointer
                var localBranchesWhereCommitShaIsHead = repository.Branches.Where(b => !b.IsRemote && b.Tip.Sha == headSha).ToList();

                var matchingCurrentBranch = !string.IsNullOrEmpty(currentBranch)
                    ? localBranchesWhereCommitShaIsHead.SingleOrDefault(b => b.CanonicalName.Replace("/heads/", "/") == currentBranch.Replace("/heads/", "/"))
                    : null;
                if (matchingCurrentBranch != null)
                {
                    log.Info($"Checking out local branch '{currentBranch}'.");
                    Commands.Checkout(repository, matchingCurrentBranch);
                }
                else if (localBranchesWhereCommitShaIsHead.Count > 1)
                {
                    var branchNames = localBranchesWhereCommitShaIsHead.Select(r => r.CanonicalName);
                    var csvNames = string.Join(", ", branchNames);
                    const string moveBranchMsg = "Move one of the branches along a commit to remove warning";

                    log.Warning($"Found more than one local branch pointing at the commit '{headSha}' ({csvNames}).");
                    var master = localBranchesWhereCommitShaIsHead.SingleOrDefault(n => n.FriendlyName == "master");
                    if (master != null)
                    {
                        log.Warning("Because one of the branches is 'master', will build master." + moveBranchMsg);
                        Commands.Checkout(repository, master);
                    }
                    else
                    {
                        var branchesWithoutSeparators = localBranchesWhereCommitShaIsHead.Where(b => !b.FriendlyName.Contains('/') && !b.FriendlyName.Contains('-')).ToList();
                        if (branchesWithoutSeparators.Count == 1)
                        {
                            var branchWithoutSeparator = branchesWithoutSeparators[0];
                            log.Warning($"Choosing {branchWithoutSeparator.CanonicalName} as it is the only branch without / or - in it. " + moveBranchMsg);
                            Commands.Checkout(repository, branchWithoutSeparator);
                        }
                        else
                        {
                            throw new WarningException("Failed to try and guess branch to use. " + moveBranchMsg);
                        }
                    }
                }
                else if (localBranchesWhereCommitShaIsHead.Count == 0)
                {
                    log.Info($"No local branch pointing at the commit '{headSha}'. Fake branch needs to be created.");
                    repository.CreateFakeBranchPointingAtThePullRequestTip(log, authentication);
                }
                else
                {
                    log.Info($"Checking out local branch 'refs/heads/{localBranchesWhereCommitShaIsHead[0].FriendlyName}'.");
                    Commands.Checkout(repository, repository.Branches[localBranchesWhereCommitShaIsHead[0].FriendlyName]);
                }
            }
            finally
            {
                if (repository.Head.Tip.Sha != expectedSha)
                {
                    if (environment.GetEnvironmentVariable("IGNORE_NORMALISATION_GIT_HEAD_MOVE") != "1")
                    {
                        // Whoa, HEAD has moved, it shouldn't have. We need to blow up because there is a bug in normalisation
                        throw new BugException($@"GitVersion has a bug, your HEAD has moved after repo normalisation.

To disable this error set an environmental variable called IGNORE_NORMALISATION_GIT_HEAD_MOVE to 1

Please run `git {CreateGitLogArgs(100)}` and submit it along with your build log (with personal info removed) in a new issue at https://github.com/GitTools/GitVersion");
                    }
                }
            }
        }

        public static string CreateGitLogArgs(int? maxCommits)
        {
            return @"log --graph --format=""%h %cr %d"" --decorate --date=relative --all --remotes=*" + (maxCommits != null ? $" -n {maxCommits}" : null);
        }
    }
}
