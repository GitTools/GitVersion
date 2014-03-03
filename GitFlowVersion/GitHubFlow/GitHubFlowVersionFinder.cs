namespace GitFlowVersion
{
    public class GitHubFlowVersionFinder
    {
        public VersionAndBranch FindVersion(GitVersionContext context)
        {
            var repositoryDirectory = context.Repository.Info.WorkingDirectory;
            var lastTaggedReleaseFinder = new LastTaggedReleaseFinder(context.Repository, repositoryDirectory);
            return new BuildNumberCalculator(new NextSemverCalculator(new NextVersionTxtFileFinder(repositoryDirectory),
                lastTaggedReleaseFinder), lastTaggedReleaseFinder, context.Repository).GetBuildNumber(context);
        }
    }
}