namespace GitVersion.Git;

/// <summary>
/// Describes the on-disk layout of a Git repository: where its git directory, common
/// directory and working directory are, and how to open its object and reference stores.
/// Handles regular repositories, bare repositories, linked worktrees (<c>.git</c> files
/// and <c>commondir</c> indirection) and shallow clones.
/// </summary>
internal sealed class GitRepositoryLayout
{
    private const string GitDirectoryOrFileName = ".git";
    private const string GitDirFilePrefix = "gitdir:";

    /// <summary>
    /// Gets the (per-worktree) git directory, which contains <c>HEAD</c>.
    /// </summary>
    public required string GitDirectory { get; init; }

    /// <summary>
    /// Gets the common git directory, which contains <c>objects/</c>, <c>refs/</c> and
    /// <c>packed-refs</c>. Equal to <see cref="GitDirectory"/> except for linked worktrees.
    /// </summary>
    public required string CommonDirectory { get; init; }

    /// <summary>
    /// Gets the root of the working directory, or <see langword="null"/> for bare repositories.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Gets the path to the object database of the repository.
    /// </summary>
    public string ObjectsDirectory => Path.Combine(CommonDirectory, "objects");

    /// <summary>
    /// Gets a value indicating whether the repository is a shallow clone.
    /// </summary>
    public bool IsShallow => File.Exists(ShallowFilePath);

    private string ShallowFilePath => Path.Combine(CommonDirectory, "shallow");

    /// <summary>
    /// Discovers the repository containing <paramref name="startPath"/> by walking up the
    /// directory hierarchy, resolving <c>.git</c> files (linked worktrees, submodules) and
    /// the <c>commondir</c> indirection.
    /// </summary>
    /// <param name="startPath">The path at which to start the discovery.</param>
    /// <returns>The layout of the discovered repository.</returns>
    /// <exception cref="GitObjectStoreException">No repository was found.</exception>
    public static GitRepositoryLayout Discover(string startPath) =>
        TryDiscover(startPath)
            ?? throw new GitObjectStoreException($"No git repository was found at or above '{startPath}'.");

    /// <summary>
    /// Discovers the repository containing <paramref name="startPath"/> by walking up the
    /// directory hierarchy, resolving <c>.git</c> files (linked worktrees, submodules) and
    /// the <c>commondir</c> indirection.
    /// </summary>
    /// <param name="startPath">The path at which to start the discovery.</param>
    /// <returns>The layout of the discovered repository, or <see langword="null"/> if none was found.</returns>
    public static GitRepositoryLayout? TryDiscover(string startPath)
    {
        ArgumentNullException.ThrowIfNull(startPath);

        for (var current = Path.GetFullPath(startPath); current is not null; current = Path.GetDirectoryName(current))
        {
            if (TryOpenExact(current) is { } layout)
            {
                return layout;
            }
        }

        return null;
    }

    /// <summary>
    /// Opens the repository at exactly <paramref name="path"/> (a working directory with a
    /// <c>.git</c> entry, or a git directory itself) without walking up the hierarchy —
    /// the equivalent of libgit2's <c>new Repository(path)</c> as opposed to its discovery.
    /// </summary>
    /// <param name="path">The path of the repository.</param>
    /// <returns>The layout of the repository, or <see langword="null"/> when the path is not itself a repository.</returns>
    public static GitRepositoryLayout? TryOpen(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return TryOpenExact(Path.GetFullPath(path));
    }

    private static GitRepositoryLayout? TryOpenExact(string current)
    {
        var dotGit = Path.Combine(current, GitDirectoryOrFileName);

        if (Directory.Exists(dotGit))
        {
            return FromGitDirectory(dotGit, current);
        }

        if (File.Exists(dotGit))
        {
            return FromGitDirectory(ResolveGitDirFile(dotGit), current);
        }

        if (IsGitDirectory(current))
        {
            // A bare repository, or the .git directory itself.
            return FromGitDirectory(current, workingDirectory: null);
        }

        return null;
    }

    /// <summary>
    /// Creates the layout for a known git directory, resolving the <c>commondir</c> indirection
    /// used by linked worktrees.
    /// </summary>
    /// <param name="gitDirectory">The git directory.</param>
    /// <param name="workingDirectory">The root of the working directory, or <see langword="null"/> for bare repositories.</param>
    /// <returns>The layout of the repository.</returns>
    public static GitRepositoryLayout FromGitDirectory(string gitDirectory, string? workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(gitDirectory);

        gitDirectory = Path.GetFullPath(gitDirectory);
        var commonDirectory = gitDirectory;

        // Linked worktrees store the path of the main repository's git directory in a 'commondir' file.
        var commonDirFile = Path.Combine(gitDirectory, "commondir");

        if (File.Exists(commonDirFile))
        {
            var target = File.ReadAllText(commonDirFile).Trim();

            commonDirectory = Path.IsPathRooted(target)
                ? Path.GetFullPath(target)
                : Path.GetFullPath(Path.Combine(gitDirectory, target));
        }

        if (Directory.Exists(Path.Combine(commonDirectory, "reftable")))
        {
            throw new NotSupportedException("Repositories using the reftable reference storage format are not supported yet.");
        }

        // SHA-256 repositories declare extensions.objectformat in their config. The managed
        // reader only implements SHA-1 pack indexes so far; fail clearly at open instead of
        // deep inside an object lookup (parity: libgit2 cannot read them either).
        var objectFormat = GitConfigurationFile.Load(Path.Combine(commonDirectory, "config"))
            .GetString("extensions", null, "objectformat");
        if (objectFormat is not null && !objectFormat.Equals("sha1", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Repositories using the '{objectFormat}' object format are not supported yet.");
        }

        return new()
        {
            GitDirectory = gitDirectory,
            CommonDirectory = commonDirectory,
            WorkingDirectory = workingDirectory
        };
    }

    /// <summary>
    /// Opens the object store of the repository.
    /// </summary>
    /// <returns>A new <see cref="GitObjectStore"/> over the repository's object database.</returns>
    public GitObjectStore CreateObjectStore() => new(ObjectsDirectory);

    /// <summary>
    /// Opens the reference store of the repository.
    /// </summary>
    /// <returns>A new <see cref="GitReferenceStore"/> over the repository's references.</returns>
    public GitReferenceStore CreateReferenceStore() => new(GitDirectory, CommonDirectory);

    /// <summary>
    /// Reads the commit ids at the boundary of a shallow clone from the <c>shallow</c> file.
    /// </summary>
    /// <returns>The shallow boundary commits, or an empty list when the repository is not shallow.</returns>
    public IReadOnlyList<GitObjectId> ReadShallowCommits()
    {
        if (!IsShallow)
        {
            return [];
        }

        return [.. File.ReadLines(ShallowFilePath, Encoding.UTF8)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .Select(GitObjectId.Parse)];
    }

    private static bool IsGitDirectory(string path) =>
        File.Exists(Path.Combine(path, "HEAD"))
        && Directory.Exists(Path.Combine(path, "objects"))
        && Directory.Exists(Path.Combine(path, "refs"));

    private static string ResolveGitDirFile(string dotGitFile)
    {
        // Format: "gitdir: <path>", where the path may be relative to the file's directory.
        var content = File.ReadAllText(dotGitFile).Trim();

        if (!content.StartsWith(GitDirFilePrefix, StringComparison.Ordinal))
        {
            throw new GitObjectStoreException($"The .git file '{dotGitFile}' is malformed: it does not start with '{GitDirFilePrefix}'.");
        }

        var target = content[GitDirFilePrefix.Length..].Trim();

        return Path.IsPathRooted(target)
            ? Path.GetFullPath(target)
            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(dotGitFile)!, target));
    }
}
