namespace GitVersion;

public interface IMutatingGitRepository : IGitRepository
{
    void CreateBranchForPullRequestBranch(AuthenticationInfo auth);
    void Checkout(string commitOrBranchSpec);
    void Fetch(string remote, IEnumerable<string> refSpecs, AuthenticationInfo auth, string? logMessage);
    void Clone(string? sourceUrl, string? workdirPath, AuthenticationInfo auth);
}
