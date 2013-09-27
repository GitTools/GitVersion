namespace GitFlowVersion
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    class ReleaseVersionFinder
    {
        public Commit Commit;
        public IRepository Repository;
        public Branch ReleaseBranch;
        public Func<Commit, bool> IsOnDevelopBranchFunc;

        public ReleaseVersionFinder()
        {
            IsOnDevelopBranchFunc = x =>
                {
                    var developBranch = Repository.DevelopBranch();
                    return Repository.IsOnBranch(developBranch, x);
                };
        }

        public VersionAndBranch FindVersion()
        {
            var version = SemanticVersionParser.FromMajorMinorPatch(ReleaseBranch.Name.Replace("release-", ""));

            version.Stability = Stability.Beta;

            var overrideTag =
                Repository
                    .SemVerTags(Commit);
                    //.FirstOrDefault(t => SemanticVersion.FromMajorMinorPatch(t.Name).Stability != Stability.Final);

            if (overrideTag != null)
            {
                var overrideVersion = overrideTag;

                if (version.Major != overrideVersion.Major ||
                    version.Minor != overrideVersion.Minor ||
                    version.Patch != overrideVersion.Patch)
                {
                    throw new Exception(string.Format("Version on override tag: {0} did not match release branch version: {1}", overrideVersion, version));
                }

                version.Stability = overrideVersion.Stability;
            }


            version.PreReleaseNumber = ReleaseBranch
                .Commits
                .SkipWhile(x => x != Commit)
                .TakeWhile(x => !IsOnDevelopBranchFunc(x))
                .Count();

            return new VersionAndBranch
                   {
                       BranchType = BranchType.Release,
                       BranchName = ReleaseBranch.Name,
                       Sha = Commit.Sha,
                       Version = version
                   };
        }
    }
}