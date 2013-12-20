namespace GitFlowVersion
{
    using System.Linq;

    class DevelopVersionFinder
    {
        public VersionAndBranch FindVersion(GitFlowVersionContext context)
        {
            var version = GetSemanticVersion(context);

            version.Minor++;
            version.Patch = 0;

            version.Stability = Stability.Unstable;

            return new VersionAndBranch
                   {
                       BranchType = BranchType.Develop,
                       BranchName = "develop",
                       Sha = context.Tip.Sha,
                       Version = version
                   };
        }

        SemanticVersion GetSemanticVersion(GitFlowVersionContext context)
        {
            var versionOnMasterFinder = new VersionOnMasterFinder();
            var versionFromMaster = versionOnMasterFinder.Execute(context, context.Tip.When());

            var developBranch = context.Repository.FindBranch("develop");
            var preReleasePartOne = developBranch.Commits
                .SkipWhile(x => x != context.Tip)
                .TakeWhile(x => x.When() >= versionFromMaster.Timestamp)
                .Count();
            return new SemanticVersion
            {
                Major = versionFromMaster.Major,
                Minor = versionFromMaster.Minor,
                PreReleasePartOne = preReleasePartOne
            };
        }


    }
}