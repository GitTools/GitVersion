namespace GitVersion
{
    public class GitHubFlowVersionFinder
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            var lastTaggedReleaseFinder = new LastTaggedReleaseFinder(context);
            var nextSemverCalculator = new NextSemverCalculator(lastTaggedReleaseFinder, context);
            return new BuildNumberCalculator(nextSemverCalculator, lastTaggedReleaseFinder, context.Repository).GetBuildNumber(context);
        }
    }
}