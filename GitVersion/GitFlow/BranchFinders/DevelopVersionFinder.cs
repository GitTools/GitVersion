namespace GitVersion
{
    using System.Linq;

    class DevelopVersionFinder
    {
        public VersionAndBranch FindVersion(GitVersionContext context)
        {
            var version = GetSemanticVersion(context);

            version.Minor++;
            version.Patch = 0;

            return new VersionAndBranch
                   {
                       BranchType = BranchType.Develop,
                       BranchName = "develop",
                       Sha = context.CurrentBranch.Tip.Sha,
                       Version = version
                   };
        }

        SemanticVersion GetSemanticVersion(GitVersionContext context)
        {
            var versionOnMasterFinder = new VersionOnMasterFinder();
            var versionFromMaster = versionOnMasterFinder.Execute(context, context.CurrentBranch.Tip.When());

            var developBranch = context.Repository.FindBranch("develop");
            var preReleasePartOne = developBranch.Commits
                .SkipWhile(x => x != context.CurrentBranch.Tip)
                .TakeWhile(x => x.When() >= versionFromMaster.Timestamp)
                .Count();
            return new SemanticVersion
            {
                Major = versionFromMaster.Major,
                Minor = versionFromMaster.Minor,
                Tag = Stability.Unstable.ToString().ToLower() + preReleasePartOne
            };
        }


    }
}