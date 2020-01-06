namespace GitVersion
{
    public interface IGitVersionFinder
    {
        SemanticVersion FindVersion(GitVersionContext context);
    }
}
