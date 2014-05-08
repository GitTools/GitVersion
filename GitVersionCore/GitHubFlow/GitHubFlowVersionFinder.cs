namespace GitVersion
{
    public class GitHubFlowVersionFinder
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            var repositoryDirectory = context.Repository.Info.WorkingDirectory;
            var lastTaggedReleaseFinder = new LastTaggedReleaseFinder(context.Repository);
            var nextVersionTxtFileFinder = new NextVersionTxtFileFinder(repositoryDirectory);
            var nextSemverCalculator = new NextSemverCalculator(nextVersionTxtFileFinder, lastTaggedReleaseFinder, context);
            return new BuildNumberCalculator(nextSemverCalculator, lastTaggedReleaseFinder, context.Repository).GetBuildNumber(context);
        }
    }
}