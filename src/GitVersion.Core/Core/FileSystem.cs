using GitVersion.Helpers;

namespace GitVersion;

internal class FileSystem : IFileSystem
{
    public void Copy(string from, string to, bool overwrite) => File.Copy(from, to, overwrite);

    public void Move(string from, string to) => File.Move(from, to);

    public bool Exists(string file) => File.Exists(file);

    public void Delete(string path) => File.Delete(path);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public void WriteAllText(string? file, string fileContents)
    {
        // Opinionated decision to use UTF8 with BOM when creating new files or when the existing
        // encoding was not easily detected due to the file not having an encoding preamble.
        var encoding = EncodingHelper.DetectEncoding(file) ?? Encoding.UTF8;
        WriteAllText(file, fileContents, encoding);
    }

    public void WriteAllText(string? file, string fileContents, Encoding encoding)
    {
        if (string.IsNullOrEmpty(file))
            throw new ArgumentNullException(nameof(file));

        File.WriteAllText(file, fileContents, encoding);
    }

    public IEnumerable<string> DirectoryEnumerateFiles(string? directory, string searchPattern, SearchOption searchOption)
    {
        if (string.IsNullOrEmpty(directory))
            throw new ArgumentNullException(nameof(directory));

        return Directory.EnumerateFiles(directory, searchPattern, searchOption);
    }

    public Stream OpenWrite(string path) => File.OpenWrite(path);

    public Stream OpenRead(string path) => File.OpenRead(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public long GetLastDirectoryWrite(string path) => new DirectoryInfo(path)
        .GetDirectories("*.*", SearchOption.AllDirectories)
        .Select(d => d.LastWriteTimeUtc)
        .DefaultIfEmpty()
        .Max()
        .Ticks;
}
