namespace GitVersion
{
    public class GitHubFlowVersionFinder
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            var repositoryDirectory = context.Repository.Info.WorkingDirectory;
            var lastTaggedReleaseFinder = new LastTaggedReleaseFinder(context);
            var nextVersionTxtFileFinder = new NextVersionTxtFileFinder(repositoryDirectory, context.Configuration);
            var nextSemverCalculator = new NextSemverCalculator(nextVersionTxtFileFinder, lastTaggedReleaseFinder, context);
            return new BuildNumberCalculator(nextSemverCalculator, lastTaggedReleaseFinder, context.Repository).GetBuildNumber(context);
        }
    }
}