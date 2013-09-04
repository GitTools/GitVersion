namespace GitFlowVersion
{
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

            version.PreRelease = ReleaseBranch
                .Commits
                .SkipWhile(x => x != Commit)
                .TakeWhile(x => !x.IsOnBranch(developBranch))
                .Count();
            return version;
        }
    }
}