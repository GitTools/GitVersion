namespace GitFlowVersion
{
    using System.Linq;
    using LibGit2Sharp;

    public class ReleaseVersionFinder
    {
        public Commit Commit { get; set; }
        public Repository Repository { get; set; }
        public Branch ReleaseBranch { get; set; }

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