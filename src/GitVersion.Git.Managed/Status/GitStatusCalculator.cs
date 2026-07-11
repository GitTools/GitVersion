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
        var index = GitIndex.Read(Path.Combine(this.layout.GitDirectory, "index"));
        var headFiles = this.treeDiff.FlattenTree(headTreeId);
        var indexEntries = index.Entries.ToDictionary(entry => entry.Path, StringComparer.Ordinal);

        var changed = new HashSet<string>(StringComparer.Ordinal);

        CompareHeadWithIndex(headFiles, indexEntries, changed);
        CompareIndexWithWorkingDirectory(workingDirectory, indexEntries, changed);
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

    private void CompareIndexWithWorkingDirectory(
        string workingDirectory,
        Dictionary<string, GitIndexEntry> indexEntries,
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
            var fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
            {
                changed.Add(path);
                continue;
            }

            if (fileInfo.Length != entry.Size || HasExecutableBitChanged(fileInfo, entry) || HashFile(fileInfo) != entry.ObjectId)
            {
                changed.Add(path);
            }
        }
    }

    private List<string> FindUntrackedFiles(string workingDirectory, Dictionary<string, GitIndexEntry> indexEntries)
    {
        var untracked = new List<string>();
        var ignoreSources = new List<(string BasePrefix, GitIgnoreRules Rules)>();

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

    private static GitObjectId HashFile(FileInfo fileInfo)
    {
        var header = Encoding.ASCII.GetBytes($"blob {fileInfo.Length}\0");
        var content = File.ReadAllBytes(fileInfo.FullName);

        var buffer = new byte[header.Length + content.Length];
        header.CopyTo(buffer, 0);
        content.CopyTo(buffer, header.Length);

        return GitObjectId.Parse(SHA1.HashData(buffer));
    }
}
