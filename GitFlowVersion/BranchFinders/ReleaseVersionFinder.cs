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
            var versionString = ReleaseBranch.Name.Replace("release-", "");
            SemanticVersion version;
            if (!SemanticVersionParser.TryParse(versionString, out version))
            {
                var message = string.Format("Could not parse '{0}' into a version", ReleaseBranch.Name);
                throw new ErrorException(message);
            }

            version.Stability = Stability.Beta;

            var overrideTag = Repository.SemVerTag(Commit);

            if (overrideTag != null)
            {
                var overrideVersion = overrideTag;

                if (version.Major != overrideVersion.Major ||
                    version.Minor != overrideVersion.Minor ||
                    version.Patch != overrideVersion.Patch)
                {
                    throw new ErrorException(string.Format("Version on override tag: {0} did not match release branch version: {1}", overrideVersion, version));
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