using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace GitVersion.Git;

/// <summary>
/// Computes the number of uncommitted changes in a working directory, matching the
/// semantics GitVersion has always used with libgit2: the number of distinct paths in
/// a diff of the HEAD tree against the index and the working directory combined —
/// untracked files included, ignored files excluded.
/// </summary>
internal sealed class GitStatusCalculator(GitRepositoryLayout layout, GitObjectStore objectStore)
{
    private readonly GitRepositoryLayout layout = layout ?? throw new ArgumentNullException(nameof(layout));
    private readonly GitTreeDiff treeDiff = new(objectStore);

    /// <summary>
    /// Counts the paths which differ between the given HEAD tree, the index, and the
    /// working directory.
    /// </summary>
    /// <param name="headTreeId">The root tree of the HEAD commit.</param>
    /// <returns>The number of changed paths.</returns>
    public int CountUncommittedChanges(GitObjectId headTreeId)
    {
        var workingDirectory = RequireWorkingDirectory();
        var indexPath = Path.Combine(this.layout.GitDirectory, "index");
        var index = GitIndex.Read(indexPath);
        var indexTimestamp = ReadUnixTimestampSeconds(indexPath);
        var headFiles = this.treeDiff.FlattenTree(headTreeId);
        var indexEntries = index.Entries.ToDictionary(entry => entry.Path, StringComparer.Ordinal);

        var changed = new HashSet<string>(StringComparer.Ordinal);

        CompareHeadWithIndex(headFiles, indexEntries, changed);
        CompareIndexWithWorkingDirectory(workingDirectory, indexEntries, indexTimestamp, changed);
        changed.UnionWith(FindUntrackedFiles(workingDirectory, indexEntries));

        return changed.Count;
    }

    /// <summary>
    /// Counts the changes the way GitVersion counts them in a repository whose HEAD is
    /// unborn (a brand-new repository): the untracked files.
    /// </summary>
    /// <returns>The number of changed paths.</returns>
    public int CountChangesInEmptyRepository()
    {
        var workingDirectory = RequireWorkingDirectory();
        var index = GitIndex.Read(Path.Combine(this.layout.GitDirectory, "index"));
        var indexEntries = index.Entries.ToDictionary(entry => entry.Path, StringComparer.Ordinal);

        return FindUntrackedFiles(workingDirectory, indexEntries).Count;
    }

    private string RequireWorkingDirectory() =>
        this.layout.WorkingDirectory
            ?? throw new GitObjectStoreException("The repository is bare: it has no working directory to compute uncommitted changes for.");

    private static void CompareHeadWithIndex(
        Dictionary<string, GitTreeEntry> headFiles,
        Dictionary<string, GitIndexEntry> indexEntries,
        HashSet<string> changed)
    {
        foreach (var (path, headEntry) in headFiles)
        {
            if (!indexEntries.TryGetValue(path, out var indexEntry))
            {
                changed.Add(path);
                continue;
            }

            if (indexEntry.ObjectId != headEntry.Sha || indexEntry.Mode != Convert.ToUInt32(headEntry.Mode, 8))
            {
                changed.Add(path);
            }
        }

        foreach (var path in indexEntries.Keys.Where(path => !headFiles.ContainsKey(path)))
        {
            changed.Add(path);
        }
    }

    private static void CompareIndexWithWorkingDirectory(
        string workingDirectory,
        Dictionary<string, GitIndexEntry> indexEntries,
        long indexTimestamp,
        HashSet<string> changed)
    {
        foreach (var (path, entry) in indexEntries)
        {
            if (entry.Stage != 0)
            {
                // A conflicted path is always a change.
                changed.Add(path);
                continue;
            }

            if (entry.AssumeValid || entry.SkipWorktree)
            {
                continue;
            }

            var filePath = Path.Combine(workingDirectory, path.Replace('/', Path.DirectorySeparatorChar));

            if (entry.IsGitLink)
            {
                // A submodule: only its presence is checked, matching what the diff exposes.
                if (!Directory.Exists(filePath))
                {
                    changed.Add(path);
                }

                continue;
            }

            if (entry.IsSymbolicLink)
            {
                // A symbolic link's blob content is the raw link target, not the target's content.
                var linkTarget = new FileInfo(filePath).LinkTarget;

                if (linkTarget is null || HashBlob(Encoding.UTF8.GetBytes(linkTarget.Replace(Path.DirectorySeparatorChar, '/'))) != entry.ObjectId)
                {
                    changed.Add(path);
                }

                continue;
            }

            var fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
            {
                changed.Add(path);
                continue;
            }

            if ((uint)fileInfo.Length != entry.Size || HasExecutableBitChanged(fileInfo, entry))
            {
                changed.Add(path);
                continue;
            }

            // Like git, trust the cached stat data: when size and modification time still match
            // the index entry, the file is clean without hashing. An entry whose timestamp is not
            // older than the index file itself is "racily clean" and must be verified by content.
            var fileTimestamp = new DateTimeOffset(fileInfo.LastWriteTimeUtc).ToUnixTimeSeconds();
            var racilyClean = entry.ModificationTimeSeconds >= indexTimestamp;

            if (!racilyClean && fileTimestamp == entry.ModificationTimeSeconds)
            {
                continue;
            }

            if (!ContentMatchesIndexEntry(fileInfo, entry))
            {
                changed.Add(path);
            }
        }
    }

    /// <summary>
    /// Verifies a file's content against the staged blob. When the raw content does not match,
    /// the CRLF-normalized content is tried as well: git's check-in filters strip carriage
    /// returns for text files, so a checkout with CRLF line endings still counts as clean —
    /// matching how libgit2 applies filters before comparing blob ids.
    /// </summary>
    private static bool ContentMatchesIndexEntry(FileInfo fileInfo, GitIndexEntry entry)
    {
        var content = File.ReadAllBytes(fileInfo.FullName);

        if (HashBlob(content) == entry.ObjectId)
        {
            return true;
        }

        var normalized = RemoveCarriageReturnsBeforeLineFeeds(content);
        return normalized is not null && HashBlob(normalized) == entry.ObjectId;
    }

    private static byte[]? RemoveCarriageReturnsBeforeLineFeeds(byte[] content)
    {
        var result = new byte[content.Length];
        var length = 0;
        var removed = false;

        for (var i = 0; i < content.Length; i++)
        {
            if (content[i] == (byte)'\r' && i + 1 < content.Length && content[i + 1] == (byte)'\n')
            {
                removed = true;
                continue;
            }

            result[length++] = content[i];
        }

        return removed ? result[..length] : null;
    }

    /// <summary>
    /// Resolves the user's global excludes file the way git does: <c>core.excludesFile</c>
    /// from the repository or global configuration, with the XDG default
    /// (<c>$XDG_CONFIG_HOME/git/ignore</c>, usually <c>~/.config/git/ignore</c>) as fallback.
    /// </summary>
    private string? ResolveGlobalExcludesFile()
    {
        static string? GetExcludesFile(string configPath) =>
            File.Exists(configPath)
                ? GitConfigurationFile.Load(configPath).GetString("core", null, "excludesfile")
                : null;

        var home = SysEnv.GetEnvironmentVariable("HOME")
            ?? SysEnv.GetFolderPath(SysEnv.SpecialFolder.UserProfile);
        var xdgConfigHome = SysEnv.GetEnvironmentVariable("XDG_CONFIG_HOME");
        var xdgBase = string.IsNullOrEmpty(xdgConfigHome) ? Path.Combine(home, ".config") : xdgConfigHome;

        // Later configuration levels override earlier ones, so the repository config wins.
        var excludesFile = GetExcludesFile(Path.Combine(this.layout.CommonDirectory, "config"))
            ?? GetExcludesFile(Path.Combine(home, ".gitconfig"))
            ?? GetExcludesFile(Path.Combine(xdgBase, "git", "config"));

        if (excludesFile is null)
        {
            return Path.Combine(xdgBase, "git", "ignore");
        }

        return excludesFile.StartsWith("~/", StringComparison.Ordinal)
            ? Path.Combine(home, excludesFile[2..])
            : excludesFile;
    }

    private static long ReadUnixTimestampSeconds(string path) =>
        File.Exists(path)
            ? new DateTimeOffset(File.GetLastWriteTimeUtc(path)).ToUnixTimeSeconds()
            : 0;

    private List<string> FindUntrackedFiles(string workingDirectory, Dictionary<string, GitIndexEntry> indexEntries)
    {
        var untracked = new List<string>();
        var ignoreSources = new List<(string BasePrefix, GitIgnoreRules Rules)>();

        // Shallowest sources first: the user's global excludes file, then the repository's
        // info/exclude, then the per-directory .gitignore chain added while walking.
        if (ResolveGlobalExcludesFile() is { } globalExcludesFile && GitIgnoreRules.Load(globalExcludesFile) is { } globalRules)
        {
            ignoreSources.Add(("", globalRules));
        }

        if (GitIgnoreRules.Load(Path.Combine(this.layout.CommonDirectory, "info", "exclude")) is { } excludeRules)
        {
            ignoreSources.Add(("", excludeRules));
        }

        WalkDirectory(workingDirectory, relativePrefix: "", ignoreSources, indexEntries, untracked);
        return untracked;
    }

    private static void WalkDirectory(
        string directory,
        string relativePrefix,
        List<(string BasePrefix, GitIgnoreRules Rules)> ignoreSources,
        Dictionary<string, GitIndexEntry> indexEntries,
        List<string> untracked)
    {
        var addedSource = false;

        if (GitIgnoreRules.Load(Path.Combine(directory, ".gitignore")) is { } rules)
        {
            ignoreSources.Add((relativePrefix, rules));
            addedSource = true;
        }

        try
        {
            foreach (var entryPath in Directory.EnumerateFileSystemEntries(directory).Order(StringComparer.Ordinal))
            {
                var name = Path.GetFileName(entryPath);

                if (relativePrefix.Length == 0 && name == ".git")
                {
                    continue;
                }

                var relativePath = relativePrefix + name;
                var isDirectory = Directory.Exists(entryPath);

                if (IsIgnored(ignoreSources, relativePath, isDirectory))
                {
                    // Ignored directories are not descended into: nothing below them
                    // can be re-included, matching git.
                    continue;
                }

                if (isDirectory)
                {
                    WalkDirectory(entryPath, relativePath + "/", ignoreSources, indexEntries, untracked);
                }
                else if (!indexEntries.ContainsKey(relativePath))
                {
                    untracked.Add(relativePath);
                }
            }
        }
        finally
        {
            if (addedSource)
            {
                ignoreSources.RemoveAt(ignoreSources.Count - 1);
            }
        }
    }

    private static bool IsIgnored(
        List<(string BasePrefix, GitIgnoreRules Rules)> ignoreSources,
        string relativePath,
        bool isDirectory)
    {
        // Deeper sources take precedence over shallower ones; .git/info/exclude is the
        // shallowest of all.
        for (var i = ignoreSources.Count - 1; i >= 0; i--)
        {
            var (basePrefix, rules) = ignoreSources[i];

            if (rules.IsIgnored(relativePath[basePrefix.Length..], isDirectory) is { } decision)
            {
                return decision;
            }
        }

        return false;
    }

    private static bool HasExecutableBitChanged(FileInfo fileInfo, GitIndexEntry entry)
    {
        // Windows has no executable bit and git ignores file modes there.
        if (OperatingSystem.IsWindows())
        {
            return false;
        }

        var isExecutable = (File.GetUnixFileMode(fileInfo.FullName) & UnixFileMode.UserExecute) != 0;
        return isExecutable != entry.IsExecutable;
    }

    [SuppressMessage("Critical Security Hotspot", "S4790:Using weak hashing algorithms is security-sensitive", Justification = "Git object ids are SHA-1 by definition; the hash is used for content identity, not security.")]
    private static GitObjectId HashBlob(byte[] content)
    {
        var header = Encoding.ASCII.GetBytes($"blob {content.Length}\0");

        var buffer = new byte[header.Length + content.Length];
        header.CopyTo(buffer, 0);
        content.CopyTo(buffer, header.Length);

        return GitObjectId.Parse(SHA1.HashData(buffer));
    }
}
