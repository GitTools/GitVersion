using GitVersion;
using LibGit2Sharp;

public static class FindersHelper
{
    public static SemanticVersion RetrieveMasterVersion(Repository repo)
    {
        var masterFinder = new MasterVersionFinder();
        var masterVersion = masterFinder.FindVersion(repo, repo.Branches["master"].Tip);
        return masterVersion;
    }
}
