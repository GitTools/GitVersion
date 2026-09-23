namespace GitVersion.Sonar;

public sealed record RunIdentity(string Repository, long RunId, int Attempt, string Event,
    string HeadSha, string AnalyzedSha, string BaseSha, int? PullRequest, string Branch, string BaseBranch)
{
    public void Validate()
    {
        SafeFiles.Require(Repository == "GitTools/GitVersion" && RunId > 0 && Attempt > 0, "Invalid source run");
        SafeFiles.Require(Event is "pull_request" or "push", "Unsupported event; merge-queue publication requires a verified mapping");
        SafeFiles.Require(new[] { HeadSha, AnalyzedSha, BaseSha }.All(s => s is not null && s.Length == 40 && s.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f')), "Invalid revision");
        SafeFiles.Require(Event == "pull_request" ? PullRequest > 0 : PullRequest is null && HeadSha == AnalyzedSha, "Invalid PR identity");
        SafeFiles.Require(!string.IsNullOrWhiteSpace(Branch) && !string.IsNullOrWhiteSpace(BaseBranch) &&
                          !Branch.Any(char.IsControl) && !BaseBranch.Any(char.IsControl), "Invalid branch");
    }

    public void EnsureCurrent(RunIdentity expected)
    {
        Validate();
        expected.Validate();
        SafeFiles.Require(this == expected, "Stale or mismatched run, revision or PR identity");
    }
}
