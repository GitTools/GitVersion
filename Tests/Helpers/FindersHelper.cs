using GitFlowVersion;
using LibGit2Sharp;

namespace Tests.Helpers
{
    public static class FindersHelper
    {
        public static VersionAndBranch RetrieveMasterVersion(Repository repo)
        {
            var masterFinder = new MasterVersionFinder
                               {
                                   Repository = repo,
                                   Commit = repo.Branches["master"].Tip
                               };
            var masterVersion = masterFinder.FindVersion();
            return masterVersion;
        }
    }
}
