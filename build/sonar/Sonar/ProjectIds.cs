using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace GitVersion.Sonar;

public static class ProjectIds
{
    public static IEnumerable<string> Projects(string repository)
    {
        var pending = new Queue<string>(new[] { "src", "new-cli", "build" }.Select(p => Path.Combine(repository, p)).Where(Directory.Exists));
        while (pending.TryDequeue(out var directory))
        {
            foreach (var entry in new DirectoryInfo(directory).EnumerateFileSystemInfos().OrderBy(p => p.Name, StringComparer.Ordinal))
            {
                if (entry.Name is "obj" or "bin" or ".git")
                {
                    continue;
                }

                SafeFiles.Require(entry.LinkTarget is null, "Linked project path");
                if (entry is DirectoryInfo)
                {
                    pending.Enqueue(entry.FullName);
                }
                else if (entry.Extension == ".csproj")
                {
                    yield return entry.FullName;
                }
            }
        }
    }

    public static Guid Identifier(string relative) => new(SHA256.HashData(Encoding.UTF8.GetBytes("GitTools/GitVersion/" + SafeFiles.Relative(relative))).AsSpan(0, 16));

    public static void Write(string repository, string destination)
    {
        var group = new XElement("PropertyGroup");
        foreach (var path in Projects(repository))
        {
            var relative = Path.GetRelativePath(repository, path).Replace('\\', '/');
            SafeFiles.Require(!path.Any(c => "'$;".Contains(c)), "Unsupported MSBuild path");
            group.Add(new XElement("ProjectGuid", new XAttribute("Condition", $"'$(MSBuildProjectFullPath)' == '{path}'"), Identifier(relative)));
        }
        new XDocument(new XElement("Project", group)).Save(destination);
    }
}
