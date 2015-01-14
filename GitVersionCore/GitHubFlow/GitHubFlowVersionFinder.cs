using System.IO;
namespace GitVersion
{
    public class GitHubFlowVersionFinder
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            var repositoryDirectory = context.Repository.Info.WorkingDirectory ?? new DirectoryInfo(context.Repository.Info.Path).Parent.Parent.FullName;            
            var lastTaggedReleaseFinder = new LastTaggedReleaseFinder(context);
            var nextVersionTxtFileFinder = new NextVersionTxtFileFinder(repositoryDirectory, context.Configuration);
            var nextSemverCalculator = new NextSemverCalculator(nextVersionTxtFileFinder, lastTaggedReleaseFinder, context);
            return new BuildNumberCalculator(nextSemverCalculator, lastTaggedReleaseFinder, context.Repository).GetBuildNumber(context);
        }
    }
}