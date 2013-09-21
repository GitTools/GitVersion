namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    public class GitFlowVersionFinder
    {
        public Commit Commit;
        public Repository Repository;
        public Branch Branch;

        public VersionAndBranch FindVersion()
        {
            if (Branch.Name == "master")
            {
                return new MasterVersionFinder
                                          {
                                              Commit = Commit,
                                          }.FindVersion();
            }

            if (Branch.Name.StartsWith("hotfix-"))
            {
                return new HotfixVersionFinder
                                          {
                                              Commit = Commit,
                                              HotfixBranch = Branch,
                                              MasterBranch = Repository.Branches.First(x => x.Name == "master")
                                          }.FindVersion();
            }

            if (Branch.Name.StartsWith("release-"))
            {
                return new ReleaseVersionFinder
                {
                    Commit = Commit,
                    Repository = Repository,
                    ReleaseBranch = Branch,
                }.FindVersion();
            }

            if (Branch.Name == "develop")
            {
                return new DevelopVersionFinder
                {
                    Commit = Commit,
                    Repository = Repository
                }.FindVersion();
            }

            if (IsPullRequest())
            {
                return new PullVersionFinder
                {
                    Commit = Commit,
                    Repository = Repository,
                    PullBranch = Branch
                }.FindVersion();
            }
            return new FeatureVersionFinder
            {
                Commit = Commit,
                Repository = Repository,
                FeatureBranch = Branch
            }.FindVersion();
        }

        bool IsPullRequest()
        {
            return Branch.CanonicalName.Contains("/pull/") || TeamCity.IsBuildingAPullRequest();
        }
    }
}