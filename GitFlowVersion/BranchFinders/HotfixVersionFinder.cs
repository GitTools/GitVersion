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
                bool isOnBranch = commit.IsOnBranch(MasterBranch);
                Debug.WriteLine(commit);
            }
            version.PreRelease = HotfixBranch
                .Commits
                .SkipWhile(x => x != Commit)
                .TakeWhile(x => !x.IsOnBranch(MasterBranch))
                .Count();
            return version;
        }
    }
}