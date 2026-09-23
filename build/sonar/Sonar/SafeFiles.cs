using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace GitVersion.Sonar;

public static class SafeFiles
{
    public const long MaxFileBytes = 128 * 1024 * 1024;
    public const long MaxTotalBytes = 1024 * 1024 * 1024;
    public const int MaxFiles = 20000;

    public static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidDataException(message);
        }
    }

    public static string Relative(string value)
    {
        Require(!string.IsNullOrWhiteSpace(value) && !Path.IsPathRooted(value) &&
                !value.Contains('\\') && !value.Contains(':') && !value.Any(char.IsControl), "Invalid relative path");
        Require(value.Split('/').All(p => p is not ("" or "." or "..")), "Invalid relative path component");
        return value;
    }

    public static string Resolve(string root, string relative)
    {
        var path = Path.GetFullPath(Path.Combine(root, Relative(relative)));
        var basePath = Path.GetFullPath(root);
        Require(path.StartsWith(basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal), "Path escapes root");
        for (var current = path; current is not null; current = Path.GetDirectoryName(current))
        {
            var info = Directory.Exists(current) ? (FileSystemInfo)new DirectoryInfo(current) : new FileInfo(current);
            Require(info.LinkTarget is null, "Linked paths are not allowed");
            if (current == basePath)
            {
                break;
            }
        }
        return path;
    }

    public static XDocument ReadXml(string path)
    {
        Require(new FileInfo(path).Length <= MaxFileBytes, "Oversized XML");
        using var reader = XmlReader.Create(path, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaxFileBytes
        });
        return XDocument.Load(reader);
    }

    public static string[] Files(string root)
    {
        Require(Directory.Exists(root) && new DirectoryInfo(root).LinkTarget is null, "Missing or linked directory");
        var files = new List<string>();
        var directories = new Queue<string>();
        directories.Enqueue(root);
        long size = 0;
        var count = 0;
        while (directories.TryDequeue(out var directory))
        {
            foreach (var entry in new DirectoryInfo(directory).EnumerateFileSystemInfos())
            {
                Require(entry.LinkTarget is null, "Linked data is not allowed");
                if (entry is DirectoryInfo)
                {
                    directories.Enqueue(entry.FullName);
                }
                else
                {
                    Require(entry is FileInfo file && file.Length <= MaxFileBytes, "Oversized file");
                    size += ((FileInfo)entry).Length;
                    files.Add(entry.FullName);
                }
                Require(++count <= MaxFiles && size <= MaxTotalBytes, "Data exceeds limits");
            }
        }
        return [.. files.Order(StringComparer.Ordinal)];
    }

    public static void Unpack(string zip, string destination)
    {
        Require(!Directory.Exists(destination) && !File.Exists(destination), "Destination already exists");
        Require(new FileInfo(zip).Length <= MaxTotalBytes, "Oversized archive");
        using var archive = ZipFile.OpenRead(zip);
        Require(archive.Entries.Count is > 0 and <= MaxFiles, "Invalid archive entry count");
        var names = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        long total = 0;
        foreach (var entry in archive.Entries)
        {
            var name = Relative(entry.FullName.TrimEnd('/'));
            Require(names.TryAdd(name, entry.FullName.EndsWith('/')), "Duplicate archive entry");
            var unixType = (entry.ExternalAttributes >> 16) & 0xf000;
            Require(unixType is 0 or 0x8000 or 0x4000, "Special archive entry");
            total += entry.Length;
            Require(entry.Length <= MaxFileBytes && total <= MaxTotalBytes, "Oversized archive entry");
        }
        foreach (var name in names.Keys)
        {
            for (var parent = Path.GetDirectoryName(name); !string.IsNullOrEmpty(parent); parent = Path.GetDirectoryName(parent))
            {
                Require(!names.TryGetValue(parent, out var isDirectory) || isDirectory, "File/directory archive collision");
            }
        }
        Extract(archive, destination);
    }

    private static void Extract(ZipArchive archive, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var entry in archive.Entries)
        {
            var path = Resolve(destination, entry.FullName.TrimEnd('/'));
            if (entry.FullName.EndsWith('/'))
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var input = entry.Open();
                using var output = new FileStream(path, FileMode.CreateNew);
                var buffer = new byte[81920];
                int read;
                long copied = 0;
                while ((read = input.Read(buffer)) > 0)
                {
                    copied += read;
                    Require(copied <= entry.Length, "Archive length mismatch");
                    output.Write(buffer, 0, read);
                }
                Require(output.Length == entry.Length, "Archive length mismatch");
            }
        }
    }
}
