using GitVersion.Agents;
using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion;

// Resolve once per execution, before preparation or cache lookup. An environment
// branch names the checkout's context; it must never become a target selector.
internal sealed class BranchInput
{
    public BranchInput(IOptions<GitVersionOptions> options, IEnvironment environment, ICurrentBuildAgent buildAgent)
    {
        var repository = options.Value.RepositoryInfo;
        if (!string.IsNullOrWhiteSpace(repository.TargetBranch))
        {
            TargetBranch = repository.TargetBranch;
            return;
        }

        var branch = environment.GetFirstNonBlankEnvironmentVariable("GIT_BRANCH", "Git_Branch");
        if (branch is not null)
        {
            ContextBranch = ParseContextBranch(branch);
            return;
        }

        var providerBranch = buildAgent.GetCurrentBranch(!string.IsNullOrWhiteSpace(repository.TargetUrl));
        TargetBranch = string.IsNullOrWhiteSpace(providerBranch) ? null : providerBranch;
    }

    public string? TargetBranch { get; }
    public ReferenceName? ContextBranch { get; }

    private static ReferenceName ParseContextBranch(string branch)
    {
        var reference = ReferenceName.FromBranchName(branch);
        var name = reference.IsRemoteBranch ? reference.WithoutRemote : reference.Friendly;
        // git check-ref-format --branch rules, without invoking git or accepting
        // its checkout-history expansion (@{-n}). Do not trim invalid whitespace.
        if (reference.IsTag || reference.IsPullRequest || !IsValidBranchName(branch) || !IsValidBranchName(name)
            || (reference.IsRemoteBranch && !reference.Friendly.Contains('/')))
        {
            throw new WarningException($"Invalid branch name '{branch}' in GIT_BRANCH/Git_Branch.");
        }

        return new ReferenceName(ReferenceName.LocalBranchPrefix + name);
    }

    private static bool IsValidBranchName(string name) => !string.IsNullOrEmpty(name)
        && name != "HEAD" && name[0] != '-' && !name.EndsWith('.')
        && !name.Contains("..", StringComparison.Ordinal) && !name.Contains("@{", StringComparison.Ordinal)
        && !name.Any(c => c <= ' ' || c == '\u007f' || "~^:?*[\\".Contains(c))
        && !name.Split('/').Any(part => part.Length == 0 || part[0] == '.' || part.EndsWith(".lock", StringComparison.Ordinal));
}
