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

        public SemanticVersion FindVersion()
        {
            var developBranch = Repository.Branches.First(b => b.Name == "develop");

            var version = SemanticVersion.FromMajorMinorPatch(ReleaseBranch.Name.Replace("release-", ""));

            version.Stage = Stage.Beta;

            var overrideTag =
                Commit.SemVerTags()
                      .FirstOrDefault(t => SemanticVersion.FromMajorMinorPatch(t.Name).Stage != Stage.Final);


            if (overrideTag != null)
            {
                var overrideVersion = SemanticVersion.FromMajorMinorPatch(overrideTag.Name);

                if (version.Major != overrideVersion.Major || version.Minor != overrideVersion.Minor ||
                    version.Patch != overrideVersion.Patch)
                {
                    throw new Exception(string.Format("Version on override tag: {0} did not match release branch version: {1}", overrideVersion, version));
                }

                version.Stage = SemanticVersion.FromMajorMinorPatch(overrideTag.Name).Stage;
            }
            
            
            version.PreRelease = ReleaseBranch
                .Commits
                .SkipWhile(x => x != Commit)
                .TakeWhile(x => !x.IsOnBranch(developBranch))
                .Count();
            return version;
        }
    }
}