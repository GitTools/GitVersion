using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LibGit2Sharp;

namespace GitFlowVersion
{
    public class HotfixVersionFinder
    {
        public Commit Commit { get; set; }
        public Repository Repository { get; set; }
        public Branch HotfixBranch { get; set; }
        public Branch MasterBranch { get; set; }

        public SemanticVersion FindVersion()
        {
            var version = SemanticVersion.FromMajorMinorPatch(HotfixBranch.Name.Replace("hotfix-", ""));
            version.Stage = Stage.Beta;

            foreach (var commit in HotfixBranch
                .Commits)
            {
                var isOnBranch = commit.IsOnBranch(MasterBranch);

                var listBranchesContainingCommit = ListBranchesContainingCommit(Repository, commit.Sha).ToList();
                Debug.WriteLine(commit);
            }
            version.PreRelease = HotfixBranch
                .Commits
                .SkipWhile(x => x != Commit)
                .TakeWhile(x => !x.IsOnBranch(MasterBranch))
                .Count();
            return version;
        }
        private IEnumerable<Branch> ListBranchesContainingCommit(Repository repo, string commitSha)
        {
            bool directBranchHasBeenFound = false;
            foreach (var branch in repo.Branches)
            {
                if (branch.Tip.Sha != commitSha)
                {
                    continue;
                }

                directBranchHasBeenFound = true;
                yield return branch;
            }

            if (directBranchHasBeenFound)
            {
                yield break;
            }

            foreach (var branch in repo.Branches)
            {
                var commits = repo.Commits.QueryBy(new Filter { Since = branch }).Where(c => c.Sha == commitSha);

                if (commits.Count() == 0)
                {
                    continue;
                }

                yield return branch;
            }
        }
    }
}