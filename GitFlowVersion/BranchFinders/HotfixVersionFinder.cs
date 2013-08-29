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

            version.PreRelease = HotfixBranch
                .Commits
                .SkipWhile(x => x != Commit)
                .TakeWhile(x => !x.IsOnBranch(MasterBranch))
                .Count();
            return version;
        }
    }
}