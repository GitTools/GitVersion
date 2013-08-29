using System.Linq;
using LibGit2Sharp;

namespace GitFlowVersion
{
    public class GitFlowVersionFinder
    {
        public Commit Commit;
        public Repository Repository;
        public Branch Branch;

        public SemanticVersion FindVersion()
        {
            if (Branch.Name == "master")
            {
                var masterVersionFinder = new MasterVersionFinder
                                          {
                                              Commit = Commit,
                                              Repository = Repository,
                                              MasterBranch = Branch
                                          };
                return masterVersionFinder.FindVersion();
            }
            if (Branch.Name.StartsWith("hotfix-"))
            {
                var masterVersionFinder = new HotfixVersionFinder
                                          {
                                              Commit = Commit,
                                              Repository = Repository,
                                              HotfixBranch = Branch,
                                              MasterBranch = Repository.Branches.First(x => x.Name == "master")
                                          };
                return masterVersionFinder.FindVersion();
            }

            if (Branch.Name.StartsWith("release-"))
            {
                var masterVersionFinder = new ReleaseVersionFinder
                {
                    Commit = Commit,
                    Repository = Repository,
                    ReleaseBranch = Branch,
                };
                return masterVersionFinder.FindVersion();
            }

            if (Branch.Name == "develop")
            {
                var masterVersionFinder = new DevelopVersionFinder
                {
                    Commit = Commit,
                    Repository = Repository
                };
                return masterVersionFinder.FindVersion();
            }
            return null;
        }

    }
}