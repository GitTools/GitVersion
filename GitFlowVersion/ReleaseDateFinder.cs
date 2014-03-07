using System.Diagnostics;
using GitFlowVersion;
using LibGit2Sharp;

public class ReleaseDateFinder
{
    public static ReleaseDate Execute(IRepository repo, VersionAndBranch vab)
    {
        var c = repo.Lookup<Commit>(vab.Sha);
        Debug.Assert(c != null);

        var rd = new ReleaseDate
                 {
                     OriginalDate = c.When(),
                     OriginalCommitSha = c.Sha,
                     Date = c.When(),
                     CommitSha = c.Sha,
                 };

        if (GitVersionFinder.ShouldGitHubFlowVersioningSchemeApply(repo))
        {
            return rd;
        }

        if (vab.Version.Patch == 0)
        {
            return rd;
        }

        var vp = new VersionOnMasterFinder().FindLatestStableTaggedCommitReachableFrom(repo, c);
        var latestStable = repo.Lookup<Commit>(vp.CommitSha);
        Debug.Assert(latestStable != null);

        rd.OriginalDate = latestStable.When();
        rd.OriginalCommitSha = vp.CommitSha;
        return rd;
    }
}
