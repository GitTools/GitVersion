using System.Text.RegularExpressions;
using LibGit2Sharp;

if (args.Length < 2)
{
    Console.WriteLine("Usage: GitRepoAnonymizer <source-repo-path> <destination-repo-path>");
    return;
}

var sourcePath = args[0];
var destPath = args[1];

try
{
    AnonymizeRepository(sourcePath, destPath);
    Console.WriteLine("Repository anonymization completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

return;

static void AnonymizeRepository(string sourcePath, string destPath)
{
    // Initialize destination repository
    if (Directory.Exists(destPath))
    {
        Console.WriteLine("Destination directory already exists. Please remove it first.");
        return;
    }

    Repository.Init(destPath);

    using var sourceRepo = new Repository(sourcePath);
    using var destRepo = new Repository(destPath);
    // Create anonymous signature
    var anonSignature = new Signature("Anonymous", "anonymous@example.com", DateTimeOffset.Now);

    // Dictionary to map old commit SHAs to new commit SHAs
    var commitMap = new Dictionary<string, string>();

    // Get all commits reachable from any ref (branches, tags, remotes) in topological order
    var commits = sourceRepo.Commits.QueryBy(new CommitFilter
    {
        IncludeReachableFrom = sourceRepo.Refs,
        SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse
    }).ToList();

    Console.WriteLine($"Processing {commits.Count} commits...");

    // Process each commit
    foreach (var commit in commits)
    {
        // Transform the commit message
        var transformedMessage = TransformCommitMessage(commit.Message);

        Console.WriteLine($"Processing commit: {commit.Sha[..7]} - {commit.MessageShort}");
        if (commit.Message != transformedMessage)
        {
            Console.WriteLine("  Message transformed");
        }

        // Get parent commits in the new repository
        var newParents = commit.Parents
            .Select(p => commitMap.TryGetValue(p.Sha, out var value) ? destRepo.Lookup<Commit>(value) : null)
            .Where(p => p != null)
            .ToList();

        // Create an empty tree (no blobs)
        var emptyTree = destRepo.ObjectDatabase.CreateTree(new TreeDefinition());

        // Create the new commit (preserve original author/committer dates)
        var anonAuthor = new Signature(anonSignature.Name, anonSignature.Email, commit.Author.When);
        var anonCommitter = new Signature(anonSignature.Name, anonSignature.Email, commit.Committer.When);
        var newCommit = destRepo.ObjectDatabase.CreateCommit(
            anonAuthor,
            anonCommitter,
            transformedMessage,
            emptyTree,
            newParents.Count != 0 ? newParents : [],
            false);

        commitMap[commit.Sha] = newCommit.Sha;
    }

    // Recreate branches
    Console.WriteLine("\nRecreating branches...");
    foreach (var branch in sourceRepo.Branches)
    {
        if (branch.Tip != null && commitMap.TryGetValue(branch.Tip.Sha, out var newCommitSha))
        {
            if (branch.IsRemote)
            {
                // Skip remote branches for simplicity
                continue;
            }

            var transformedBranchName = TransformBranchName(branch.FriendlyName);

            try
            {
                if (branch.FriendlyName is "master" or "main")
                {
                    // Create branch and update HEAD to point to it
                    destRepo.Refs.Add($"refs/heads/{transformedBranchName}", newCommitSha);
                    destRepo.Refs.UpdateTarget(destRepo.Refs.Head, $"refs/heads/{transformedBranchName}");
                    Console.WriteLine($"  Created branch: {transformedBranchName}");
                }
                else
                {
                    destRepo.Refs.Add($"refs/heads/{transformedBranchName}", newCommitSha);
                    if (branch.FriendlyName != transformedBranchName)
                    {
                        Console.WriteLine($"  Created branch: {branch.FriendlyName} → {transformedBranchName}");
                    }
                    else
                    {
                        Console.WriteLine($"  Created branch: {transformedBranchName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Warning: Could not create branch {transformedBranchName}: {ex.Message}");
            }
        }
    }

    // Recreate tags
    Console.WriteLine("\nRecreating tags...");
    foreach (var tag in sourceRepo.Tags)
    {
        var targetCommit = tag.Target as Commit ?? (tag.Target as TagAnnotation)?.Target as Commit;

        if (targetCommit != null && commitMap.TryGetValue(targetCommit.Sha, out var newCommitSha))
        {
            var transformedTagName = TransformTagName(tag.FriendlyName);

            try
            {
                if (tag.IsAnnotated)
                {
                    var transformedTagMessage = TransformCommitMessage(tag.Annotation.Message);
                    var anonTagger = new Signature(anonSignature.Name, anonSignature.Email, tag.Annotation.Tagger.When);
                    destRepo.ApplyTag(transformedTagName, newCommitSha, anonTagger, transformedTagMessage);
                }
                else
                {
                    destRepo.ApplyTag(transformedTagName, newCommitSha);
                }

                Console.WriteLine(tag.FriendlyName != transformedTagName
                    ? $"  Created tag: {tag.FriendlyName} → {transformedTagName}"
                    : $"  Created tag: {transformedTagName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Warning: Could not create tag {transformedTagName}: {ex.Message}");
            }
        }
    }

    Console.WriteLine($"\nTotal commits created: {commitMap.Count}");
}

static string TransformCommitMessage(string originalMessage)
{
    if (string.IsNullOrWhiteSpace(originalMessage))
        return originalMessage;

    var transformed = originalMessage;

    // Remove email addresses
    transformed = RegexPatterns.EmailPattern.Replace(transformed, "[EMAIL]");

    // Remove potential usernames (e.g., @username)
    transformed = RegexPatterns.UsernamePattern.Replace(transformed, "@[USER]");

    // Remove URLs
    transformed = RegexPatterns.UrlPattern.Replace(transformed, "[URL]");

    // Remove IP addresses
    transformed = RegexPatterns.IpAddressPattern.Replace(transformed, "[IP]");

    // Remove potential API keys or tokens (common patterns)
    transformed = RegexPatterns.TokenPattern.Replace(transformed, "[TOKEN]");

    // Remove JIRA/ticket references (e.g., PROJ-123, ABC-456)
    transformed = RegexPatterns.TicketPattern.Replace(transformed, "[TICKET]");

    // Remove file paths (optional - uncomment if needed)
    // transformed = Regex.Replace(transformed, @"[/\\][\w/\\.-]+", "[PATH]");

    return transformed;
}

static string TransformBranchName(string originalName)
{
    if (string.IsNullOrWhiteSpace(originalName))
        return originalName;

    var transformed = originalName;

    // Keep common branch names unchanged
    var commonBranches = new[] { "master", "main", "develop", "development", "staging", "production" };
    if (commonBranches.Contains(transformed.ToLower()))
        return transformed;

    // Remove usernames from branch names (e.g., feature/john/my-feature)
    transformed = RegexPatterns.BranchUsernamePattern.Replace(transformed, "/user/");

    // Remove JIRA/ticket references
    transformed = RegexPatterns.TicketPatternIgnoreCase.Replace(transformed, "TICKET");

    // Replace email-like patterns in branch names
    transformed = RegexPatterns.EmailLikePattern.Replace(transformed, "user");

    // Optional: completely anonymize feature branches (uncomment if needed)
    // if (transformed.StartsWith("feature/") || transformed.StartsWith("bugfix/"))
    // {
    //     var prefix = transformed.Split('/')[0];
    //     return $"{prefix}/anonymous-branch";
    // }

    return transformed;
}

static string TransformTagName(string originalName)
{
    if (string.IsNullOrWhiteSpace(originalName))
        return originalName;

    var transformed = originalName;

    // Keep version tags unchanged (e.g., v1.0.0, 1.2.3, v2.0.0-beta)
    if (RegexPatterns.VersionTagPattern.IsMatch(transformed))
        return transformed;

    // Remove JIRA/ticket references
    transformed = RegexPatterns.TicketPatternIgnoreCase.Replace(transformed, "TICKET");

    // Remove usernames
    transformed = RegexPatterns.TagUsernamePattern.Replace(transformed, "-user-");

    // Replace email-like patterns
    transformed = RegexPatterns.EmailLikePattern.Replace(transformed, "user");

    // Optional: completely anonymize non-version tags (uncomment if needed)
    // if (!Regex.IsMatch(originalName, @"^v?\d+\.\d+"))
    // {
    //     return "anonymous-tag";
    // }

    return transformed;
}

internal static partial class RegexPatterns
{
    // Email addresses
    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b")]
    public static partial Regex EmailPattern { get; }

    // Usernames (e.g., @username)
    [GeneratedRegex(@"@\w+")]
    public static partial Regex UsernamePattern { get; }

    // URLs
    [GeneratedRegex(@"https?://[^\s]+")]
    public static partial Regex UrlPattern { get; }

    // IP addresses
    [GeneratedRegex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b")]
    public static partial Regex IpAddressPattern { get; }

    // API keys or tokens (32+ characters)
    [GeneratedRegex(@"\b[A-Za-z0-9]{32,}\b")]
    public static partial Regex TokenPattern { get; }

    // JIRA/ticket references (e.g., PROJ-123, ABC-456)
    [GeneratedRegex(@"\b[A-Z]{2,}-\d+\b")]
    public static partial Regex TicketPattern { get; }

    // JIRA/ticket references (case insensitive)
    [GeneratedRegex(@"\b[A-Z]{2,}-\d+\b", RegexOptions.IgnoreCase)]
    public static partial Regex TicketPatternIgnoreCase { get; }

    // Usernames in branch names (e.g., feature/john/my-feature)
    [GeneratedRegex(@"/([\w.-]+)/")]
    public static partial Regex BranchUsernamePattern { get; }

    // Email-like patterns in branch/tag names
    [GeneratedRegex("[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+")]
    public static partial Regex EmailLikePattern { get; }

    // Version tags (e.g., v1.0.0, 1.2.3, v2.0.0-beta)
    [GeneratedRegex(@"^v?\d+\.\d+(\.\d+)?(-[\w.]+)?$")]
    public static partial Regex VersionTagPattern { get; }

    // Usernames in tags (e.g., -username-)
    [GeneratedRegex(@"[-_/]([\w]+)[-_/]")]
    public static partial Regex TagUsernamePattern { get; }
}
