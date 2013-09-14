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

        public VersionInformation FindVersion()
        {
            var developBranch = Repository.DevelopBranch();

            var version = VersionInformation.FromMajorMinorPatch(ReleaseBranch.Name.Replace("release-", ""));

            version.Stability = Stability.Beta;
            version.BranchType = BranchType.Release;
            version.BranchName = ReleaseBranch.Name;
            version.Sha = Commit.Sha;

            var overrideTag =
                Commit.SemVerTags()
                      .FirstOrDefault(t => VersionInformation.FromMajorMinorPatch(t.Name).Stability != Stability.Final);


            if (overrideTag != null)
            {
                var overrideVersion = VersionInformation.FromMajorMinorPatch(overrideTag.Name);

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
            return version;
        }
    }
}