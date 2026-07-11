namespace GitVersion.Git.Managed.Tests;

/// <summary>
/// Creates a real Git repository in a temporary directory by invoking the <c>git</c>
/// command-line executable, with deterministic author/committer identities and dates.
/// </summary>
internal sealed class GitTestRepository : IDisposable
{
    public const string AuthorName = "Test Author";
    public const string AuthorEmail = "author@example.com";
    public const string CommitterName = "Test Committer";
    public const string CommitterEmail = "committer@example.com";

    private static readonly DateTimeOffset BaseDate = new(2023, 6, 1, 10, 0, 0, TimeSpan.FromHours(2));

    private int ticks;

    public GitTestRepository()
    {
        RepositoryPath = Path.Combine(Path.GetTempPath(), "managed-git-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RepositoryPath);
        Run("init", "-q", "-b", "main");
    }

    public string RepositoryPath { get; }

    public string ObjectsDirectory => Path.Combine(RepositoryPath, ".git", "objects");

    /// <summary>
    /// Gets the date used for the author and the committer of the most recent commit or tag.
    /// </summary>
    public DateTimeOffset CurrentDate => BaseDate.AddMinutes(this.ticks);

    /// <summary>
    /// Runs <c>git</c> with the given arguments in the repository and returns its standard output.
    /// </summary>
    public string Run(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = RepositoryPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var date = $"{CurrentDate.ToUnixTimeSeconds()} +0200";
        startInfo.Environment["GIT_AUTHOR_NAME"] = AuthorName;
        startInfo.Environment["GIT_AUTHOR_EMAIL"] = AuthorEmail;
        startInfo.Environment["GIT_AUTHOR_DATE"] = date;
        startInfo.Environment["GIT_COMMITTER_NAME"] = CommitterName;
        startInfo.Environment["GIT_COMMITTER_EMAIL"] = CommitterEmail;
        startInfo.Environment["GIT_COMMITTER_DATE"] = date;
        startInfo.Environment["GIT_CONFIG_GLOBAL"] = Path.Combine(RepositoryPath, "no-global-config");
        startInfo.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";

        using var process = new Process();
        process.StartInfo = startInfo;
        process.Start();

        var standardOutput = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"'git {string.Join(' ', arguments)}' failed with exit code {process.ExitCode}: {standardError}");
        }

        return standardOutput.TrimEnd('\n');
    }

    /// <summary>
    /// Writes a file (relative to the repository root), creating parent directories as needed.
    /// </summary>
    public void WriteFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(RepositoryPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    /// <summary>
    /// Stages all changes and creates a commit with a deterministic, unique date. Returns the commit sha.
    /// </summary>
    public string Commit(string message)
    {
        this.ticks++;
        Run("add", "--all");
        Run("commit", "-q", "--no-verify", "-m", message);
        return RevParse("HEAD");
    }

    /// <summary>
    /// Creates a commit whose message is taken verbatim from the given bytes.
    /// </summary>
    public string CommitWithMessageBytes(byte[] messageBytes, string? commitEncoding = null)
    {
        this.ticks++;
        var messageFile = Path.Combine(RepositoryPath, "..", $"message-{Guid.NewGuid():N}.txt");
        File.WriteAllBytes(messageFile, messageBytes);

        try
        {
            Run("add", "--all");
            var arguments = new List<string>();
            if (commitEncoding is not null)
            {
                arguments.AddRange(["-c", $"i18n.commitEncoding={commitEncoding}"]);
            }

            arguments.AddRange(["commit", "-q", "--no-verify", "--allow-empty", "-F", messageFile]);
            Run([.. arguments]);
            return RevParse("HEAD");
        }
        finally
        {
            File.Delete(messageFile);
        }
    }

    public string RevParse(string reference) => Run("rev-parse", reference);

    public GitObjectId ResolveId(string reference) => GitObjectId.Parse(RevParse(reference));

    public GitObjectStore OpenObjectStore() => new(ObjectsDirectory);

    public void Dispose()
    {
        try
        {
            Directory.Delete(RepositoryPath, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup of the temporary directory.
        }
        catch (UnauthorizedAccessException)
        {
            // Best effort cleanup of the temporary directory.
        }
    }
}
