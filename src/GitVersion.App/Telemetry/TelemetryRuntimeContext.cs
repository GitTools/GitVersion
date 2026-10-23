using System.IO.Abstractions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace GitVersion.Telemetry;

/// <summary>Contains public runtime release information without host or repository identifiers.</summary>
internal sealed record TelemetryRuntimeContext(string Container, string Distribution, string OsVersion, string GitVersionVersion)
{
    /// <summary>Labels public metadata that could not be determined safely.</summary>
    internal const string UnknownLabel = "unknown";

    /// <summary>Represents unavailable runtime information.</summary>
    internal static readonly TelemetryRuntimeContext Unknown = new(UnknownLabel, UnknownLabel, UnknownLabel, UnknownLabel);
}

/// <summary>Reads bounded public OS metadata only for selected, eligible telemetry events.</summary>
internal sealed partial class TelemetryRuntimeContextReader(IFileSystem fileSystem, IEnvironment environment, string os, Version version)
{
    private const string UnknownLabel = TelemetryRuntimeContext.UnknownLabel;
    private static readonly HashSet<string> Distributions = new(StringComparer.Ordinal)
    {
        "debian", "ubuntu", "alpine", "fedora", "rhel", "centos", "rocky", "almalinux", "amzn",
        "opensuse-leap", "opensuse-tumbleweed", "sles", "arch", "gentoo", "nixos", "photon", "mariner", "azurelinux"
    };

    /// <summary>Returns independent container, userland OS release, and tool version dimensions.</summary>
    internal TelemetryRuntimeContext Read()
    {
        var container = ReadContainer();
        var (distribution, release) = ReadOperatingSystem();
        var toolVersion = NormalizeToolVersion(typeof(TelemetryRuntimeContextReader).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
        return new(container, distribution, release, toolVersion);
    }

    /// <summary>Recognizes explicit image markers and conventional container hints without reading container IDs.</summary>
    private string ReadContainer()
    {
        try
        {
            if (environment.GetEnvironmentVariable("GITVERSION_CONTAINER_KIND") == "docker" ||
                os == "linux" && fileSystem.File.Exists("/.dockerenv"))
            {
                return "docker";
            }
            var hint = environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
            return hint == "1" || string.Equals(hint, "true", StringComparison.OrdinalIgnoreCase) ? "other" : "none";
        }
        catch (Exception)
        {
            return UnknownLabel;
        }
    }

    /// <summary>Reads the OS userland release; a container's host release is never inspected.</summary>
    private (string Distribution, string Release) ReadOperatingSystem()
    {
        try
        {
            return os switch
            {
                "linux" => ParseLinux(ReadBounded(fileSystem.File.Exists("/etc/os-release") ? "/etc/os-release" : "/usr/lib/os-release")),
                "macos" => ("macos", ParseMacOs(ReadBounded("/System/Library/CoreServices/SystemVersion.plist"))),
                "windows" => ("windows", NormalizeRelease($"{version.Major}.{version.Minor}.{Math.Max(0, version.Build)}")),
                _ => ("other", UnknownLabel)
            };
        }
        catch (Exception)
        {
            return (os is "macos" or "windows" ? os : UnknownLabel, UnknownLabel);
        }
    }

    /// <summary>Rejects oversized metadata rather than parsing a truncated file.</summary>
    private string ReadBounded(string path)
    {
        using var reader = fileSystem.File.OpenText(path);
        var buffer = new char[8193];
        var length = reader.ReadBlock(buffer, 0, buffer.Length);
        return length > 8192 ? string.Empty : new string(buffer, 0, length);
    }

    /// <summary>Reads only allowlisted distro IDs and numeric VERSION_ID values without shell evaluation.</summary>
    internal static (string Distribution, string Release) ParseLinux(string text)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in text.Split('\n'))
        {
            var pair = line.Trim().Split('=', 2);
            if (pair.Length != 2 || pair[0] is not ("ID" or "VERSION_ID"))
            {
                continue;
            }
            if (!values.TryAdd(pair[0], pair[1].Trim().Trim('"', '\'')))
            {
                return (UnknownLabel, UnknownLabel);
            }
        }
        if (!values.TryGetValue("ID", out var id))
        {
            return (UnknownLabel, UnknownLabel);
        }
        return Distributions.Contains(id)
            ? (id, NormalizeRelease(values.GetValueOrDefault("VERSION_ID")))
            : ("other", UnknownLabel);
    }

    /// <summary>Extracts macOS ProductVersion, without reading Darwin or device/build identifiers.</summary>
    internal static string ParseMacOs(string text)
    {
        try
        {
            using var input = new StringReader(text);
            using var reader = XmlReader.Create(input, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null, MaxCharactersInDocument = 8192 });
            var entries = XDocument.Load(reader).Root?.Element("dict")?.Elements().ToArray() ?? [];
            var versions = entries.Where(x => x.Name == "key" && x.Value == "ProductVersion")
                .Select(x => x.ElementsAfterSelf().FirstOrDefault()).ToArray();
            return versions.Length == 1 && versions[0]?.Name == "string" ? NormalizeRelease(versions[0]?.Value) : UnknownLabel;
        }
        catch (Exception)
        {
            return UnknownLabel;
        }
    }

    /// <summary>Limits OS release data to at most four bounded numeric components.</summary>
    private static string NormalizeRelease(string? value) => value is { Length: <= 32 } && NumericRelease().IsMatch(value) ? value : UnknownLabel;

    /// <summary>Removes build metadata and rejects private branch names or malformed tool versions.</summary>
    internal static string NormalizeToolVersion(string? value)
    {
        var release = value?.Split('+', 2)[0];
        return release is { Length: <= 64 } && ToolRelease().IsMatch(release) ? release : UnknownLabel;
    }

    /// <summary>Matches public numeric OS release identifiers.</summary>
    [GeneratedRegex(@"\A[0-9]{1,8}(?:\.[0-9]{1,8}){0,3}\z", RegexOptions.CultureInvariant)]
    private static partial Regex NumericRelease();

    /// <summary>Matches release versions and known public prerelease channels.</summary>
    [GeneratedRegex(@"\A(?:0|[1-9][0-9]{0,7})\.(?:0|[1-9][0-9]{0,7})\.(?:0|[1-9][0-9]{0,7})(?:-(?:alpha|beta|rc|preview|dev|ci)(?:\.[0-9]{1,8})*)?\z", RegexOptions.CultureInvariant)]
    private static partial Regex ToolRelease();
}
