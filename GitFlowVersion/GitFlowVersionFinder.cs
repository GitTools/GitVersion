using System;
using LibGit2Sharp;

namespace GitFlowVersion
{
    public class GitFlowVersionFinder
    {
        public Commit Commit;
        public Repository Repository;
        public Branch Branch;

        public Version FindVersion()
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
            return null;
        }

    }
}