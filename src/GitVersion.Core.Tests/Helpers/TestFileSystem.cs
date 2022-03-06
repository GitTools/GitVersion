using GitVersion.Helpers;

namespace GitVersion.Core.Tests.Helpers;

public class TestFileSystem : IFileSystem
{
    private readonly Dictionary<string, byte[]> fileSystem = new(StringComparerUtils.OsDependentComparer);

    public void Copy(string from, string to, bool overwrite)
    {
        var fromPath = Path.GetFullPath(from);
        var toPath = Path.GetFullPath(to);
        if (this.fileSystem.ContainsKey(toPath))
        {
            if (overwrite)
                this.fileSystem.Remove(toPath);
            else
                throw new IOException("File already exists");
        }

        if (!this.fileSystem.TryGetValue(fromPath, out var source))
            throw new FileNotFoundException($"The source file '{fromPath}' was not found", from);

        this.fileSystem.Add(toPath, source);
    }

    public void Move(string from, string to)
    {
        var fromPath = Path.GetFullPath(from);
        Copy(from, to, false);
        this.fileSystem.Remove(fromPath);
    }

    public bool Exists(string file)
    {
        var path = Path.GetFullPath(file);
        return this.fileSystem.ContainsKey(path);
    }

    public void Delete(string path)
    {
        var fullPath = Path.GetFullPath(path);
        this.fileSystem.Remove(fullPath);
    }

    public string ReadAllText(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!this.fileSystem.TryGetValue(fullPath, out var content))
            throw new FileNotFoundException($"The file '{fullPath}' was not found", fullPath);

        var encoding = EncodingHelper.DetectEncoding(content) ?? Encoding.UTF8;
        return encoding.GetString(content);
    }

    public void WriteAllText(string? file, string fileContents)
    {
        var path = Path.GetFullPath(file ?? throw new ArgumentNullException(nameof(file)));
        var encoding = this.fileSystem.ContainsKey(path)
            ? EncodingHelper.DetectEncoding(this.fileSystem[path]) ?? Encoding.UTF8
            : Encoding.UTF8;
        WriteAllText(path, fileContents, encoding);
    }

    public void WriteAllText(string? file, string fileContents, Encoding encoding)
    {
        var path = Path.GetFullPath(file ?? throw new ArgumentNullException(nameof(file)));
        this.fileSystem[path] = encoding.GetBytes(fileContents);
    }

    public IEnumerable<string> DirectoryEnumerateFiles(string? directory, string searchPattern, SearchOption searchOption) => throw new NotImplementedException();

    public Stream OpenWrite(string path) => new TestStream(path, this);

    public Stream OpenRead(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!this.fileSystem.ContainsKey(fullPath))
            throw new FileNotFoundException("File not found.", fullPath);

        var content = this.fileSystem[fullPath];
        return new MemoryStream(content);
    }

    public void CreateDirectory(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (this.fileSystem.ContainsKey(fullPath))
        {
            this.fileSystem[fullPath] = Array.Empty<byte>();
        }
        else
        {
            this.fileSystem.Add(fullPath, Array.Empty<byte>());
        }
    }

    public bool DirectoryExists(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return this.fileSystem.ContainsKey(fullPath);
    }

    public long GetLastDirectoryWrite(string path) => 1;

    public bool PathsEqual(string? path, string? otherPath) => string.Equals(
        Path.GetFullPath(path ?? throw new ArgumentNullException(nameof(path))).TrimEnd('\\').TrimEnd('/'),
        Path.GetFullPath(otherPath ?? throw new ArgumentNullException(nameof(otherPath))).TrimEnd('\\').TrimEnd('/'),
        StringComparerUtils.OsDependentComparison);
}
