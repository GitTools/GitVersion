using GitVersion;
using LibGit2Sharp;

public static class FindersHelper
{
    public static SemanticVersion RetrieveMasterVersion(Repository repo, Config configuration)
    {
        var masterFinder = new MasterVersionFinder();
        var masterVersion = masterFinder.FindVersion(new GitVersionContext(repo, repo.Branches["master"], configuration));
        return masterVersion;
    }
}
