using GitFlowVersion;
using LibGit2Sharp;

namespace Tests.Helpers
{
    public static class FindersHelper
    {
        public static VersionAndBranch RetrieveMasterVersion(Repository repo)
        {
            var masterFinder = new MasterVersionFinder();
            var masterVersion = masterFinder.FindVersion(new GitFlowVersionContext
            {
                Repository = repo,
                Tip = repo.Branches["master"].Tip
            });
            return masterVersion;
        }
    }
}
