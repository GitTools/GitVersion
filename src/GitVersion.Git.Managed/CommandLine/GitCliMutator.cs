using System.Text.RegularExpressions;
using GitVersion.Extensions;

namespace GitVersion.Git;

/// <summary>
/// Performs the mutating and network Git operations GitVersion needs (repository normalization)
/// by invoking the <c>git</c> command-line executable instead of libgit2.
/// </summary>
internal interface IGitCliMutator
{
    void Clone(string? sourceUrl, string? workdirPath, AuthenticationInfo auth);
    void Fetch(string workingDirectory, string remote, IEnumerable<string> refSpecs, AuthenticationInfo auth);
    void Checkout(string workingDirectory, string commitOrBranchSpec);
    IReadOnlyList<GitRemoteReference> ListRemoteReferences(string workingDirectory, string remoteName, AuthenticationInfo auth);
    void SetConfig(string workingDirectory, string key, string value);
    void AddConfig(string workingDirectory, string key, string value);
    void RemoveRemote(string workingDirectory, string remoteName);
    void UpdateReference(string workingDirectory, string name, string targetSha);
    void CreateSymbolicReference(string workingDirectory, string name, string targetReferenceName);
}

internal sealed partial class GitCliMutator(ILogger<GitCliMutator> logger, IGitCliExecutor executor) : IGitCliMutator
{
    private readonly ILogger<GitCliMutator> logger = logger.NotNull();
    private readonly IGitCliExecutor executor = executor.NotNull();

    public void Clone(string? sourceUrl, string? workdirPath, AuthenticationInfo auth)
    {
        ArgumentNullException.ThrowIfNull(sourceUrl);
        ArgumentNullException.ThrowIfNull(workdirPath);

        var arguments = new List<string>();
        AddAuthentication(arguments, sourceUrl, auth);
        arguments.AddRange(["clone", "--no-checkout", sourceUrl, workdirPath]);

        var result = this.executor.Execute(null, arguments);
        ThrowOnFailure(result, isNetworkOperation: true);
        this.logger.LogInformation("Cloned repository '{SourceUrl}' into '{WorkdirPath}'", sourceUrl, workdirPath);
    }

    public void Fetch(string workingDirectory, string remote, IEnumerable<string> refSpecs, AuthenticationInfo auth)
    {
        var arguments = new List<string>();
        AddAuthentication(arguments, sourceUrl: null, auth);
        arguments.AddRange(["fetch", remote]);
        arguments.AddRange(refSpecs);

        var result = this.executor.Execute(workingDirectory, arguments);
        ThrowOnFailure(result, isNetworkOperation: true);
    }

    public void Checkout(string workingDirectory, string commitOrBranchSpec)
    {
        // libgit2's Commands.Checkout attaches HEAD when given a canonical local branch name,
        // while `git checkout refs/heads/x` would detach — use the branch shortname instead.
        // `--no-guess` suppresses git's remote-branch DWIM, which libgit2 does not have.
        const string localBranchPrefix = "refs/heads/";
        var spec = commitOrBranchSpec.StartsWith(localBranchPrefix, StringComparison.Ordinal)
            ? commitOrBranchSpec[localBranchPrefix.Length..]
            : commitOrBranchSpec;

        var result = this.executor.Execute(workingDirectory, ["checkout", "--no-guess", spec]);
        ThrowOnFailure(result);
    }

    public IReadOnlyList<GitRemoteReference> ListRemoteReferences(string workingDirectory, string remoteName, AuthenticationInfo auth)
    {
        var arguments = new List<string>();
        AddAuthentication(arguments, sourceUrl: null, auth);
        arguments.AddRange(["ls-remote", "--quiet", remoteName]);

        var result = this.executor.Execute(workingDirectory, arguments);
        ThrowOnFailure(result, isNetworkOperation: true);

        var references = new List<GitRemoteReference>();
        foreach (var line in result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = line.IndexOf('\t');
            if (separator <= 0)
            {
                continue;
            }

            var sha = line[..separator];
            var name = line[(separator + 1)..];

            // Skip peeled entries ("<ref>^{}") and the symbolic HEAD advertisement: neither is
            // part of libgit2's ListReferences result that this replaces, and HEAD would show up
            // as a duplicate of the branch it points at.
            if (name.EndsWith("^{}", StringComparison.Ordinal) || !name.StartsWith("refs/", StringComparison.Ordinal))
            {
                continue;
            }

            references.Add(new(name, sha));
        }

        return references;
    }

    public void SetConfig(string workingDirectory, string key, string value) =>
        ThrowOnFailure(this.executor.Execute(workingDirectory, ["config", key, value]));

    public void AddConfig(string workingDirectory, string key, string value) =>
        ThrowOnFailure(this.executor.Execute(workingDirectory, ["config", "--add", key, value]));

    public void RemoveRemote(string workingDirectory, string remoteName) =>
        ThrowOnFailure(this.executor.Execute(workingDirectory, ["remote", "remove", remoteName]));

    public void UpdateReference(string workingDirectory, string name, string targetSha) =>
        ThrowOnFailure(this.executor.Execute(workingDirectory, ["update-ref", name, targetSha]));

    public void CreateSymbolicReference(string workingDirectory, string name, string targetReferenceName) =>
        ThrowOnFailure(this.executor.Execute(workingDirectory, ["symbolic-ref", name, targetReferenceName]));

    private static void AddAuthentication(List<string> arguments, string? sourceUrl, AuthenticationInfo auth)
    {
        if (auth.Username.IsNullOrWhiteSpace())
        {
            return;
        }

        // Same credential semantics as the libgit2 UsernamePasswordCredentials provider: basic auth
        // over HTTPS. Passed per invocation via configuration so it is never persisted and never
        // appears in the remote URL. ArgumentList passes it without shell interpolation, and the
        // value is not echoed by git in error output.
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{auth.Username}:{auth.Password ?? string.Empty}"));
        var scope = sourceUrl == null ? "http" : $"http.{sourceUrl}";
        arguments.AddRange(["-c", $"{scope}.extraHeader=Authorization: Basic {credentials}"]);
    }

    private static void ThrowOnFailure(GitCliResult result, bool isNetworkOperation = false)
    {
        if (result.IsSuccess)
        {
            return;
        }

        var message = result.StandardError;
        if (IsLockContention(message))
        {
            throw new LockedFileException(message);
        }

        // HTTP status classification only applies to commands that talk to a remote;
        // a local ref or pathspec can legitimately contain "401"/"404" in its name.
        if (isNetworkOperation)
        {
            if (message.Contains("401") || message.Contains("Authentication failed", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Unauthorized: Incorrect username/password");
            }

            if (message.Contains("403"))
            {
                throw new InvalidOperationException("Forbidden: Possibly Incorrect username/password");
            }

            // "Repository not found" is GitHub's server-side banner; other hosts produce
            // git's own "fatal: repository '<url>' not found" with the URL in between.
            if (message.Contains("404")
                || message.Contains("Repository not found", StringComparison.OrdinalIgnoreCase)
                || RepositoryNotFoundRegex().IsMatch(message))
            {
                throw new InvalidOperationException("Not found: The repository was not found");
            }
        }

        throw new InvalidOperationException($"Git command failed with exit code {result.ExitCode}: {message}");
    }

    private static bool IsLockContention(string message) =>
        (message.Contains(".lock", StringComparison.Ordinal) && message.Contains("Unable to create", StringComparison.OrdinalIgnoreCase))
        || message.Contains("could not lock", StringComparison.OrdinalIgnoreCase)
        || message.Contains("cannot lock ref", StringComparison.OrdinalIgnoreCase)
        || message.Contains("Another git process seems to be running", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"repository '[^']*' not found", RegexOptions.IgnoreCase)]
    private static partial Regex RepositoryNotFoundRegex();
}
