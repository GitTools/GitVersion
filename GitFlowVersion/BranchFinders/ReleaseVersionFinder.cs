namespace GitFlowVersion
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    class ReleaseVersionFinder
    {
        public Commit Commit;
        public Repository Repository;
        public Branch ReleaseBranch;

        public VersionAndBranch FindVersion()
        {
            var developBranch = Repository.DevelopBranch();

            var version = SemanticVersion.FromMajorMinorPatch(ReleaseBranch.Name.Replace("release-", ""));

            version.Stability = Stability.Beta;

            var overrideTag =
                Commit.SemVerTags()
                      .FirstOrDefault(t => SemanticVersion.FromMajorMinorPatch(t.Name).Stability != Stability.Final);

            if (overrideTag != null)
            {
                var overrideVersion = SemanticVersion.FromMajorMinorPatch(overrideTag.Name);

                if (version.Major != overrideVersion.Major || version.Minor != overrideVersion.Minor ||
                    version.Patch != overrideVersion.Patch)
                {
                    throw new Exception(string.Format("Version on override tag: {0} did not match release branch version: {1}", overrideVersion, version));
                }

                version.Stability = overrideVersion.Stability;
            }
            
            
            version.PreReleaseNumber = ReleaseBranch
                .Commits
                .SkipWhile(x => x != Commit)
                .TakeWhile(x => !x.IsOnBranch(developBranch))
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