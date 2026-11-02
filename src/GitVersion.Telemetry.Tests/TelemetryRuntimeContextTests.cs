using System.IO.Abstractions;
using System.Text;
using GitVersion.Telemetry;
using GitVersion.Tests;

namespace GitVersion.Telemetry.Tests;

/// <summary>Verifies public runtime metadata parsing and isolation from private machine values.</summary>
public class TelemetryRuntimeContextTests
{
    /// <summary>Retains supported distro releases while rejecting custom identifiers and free-form versions.</summary>
    [TestCase("ID=ubuntu\nVERSION_ID=\"24.04\"", "ubuntu", "24.04")]
    [TestCase("ID=alpine\nVERSION_ID=3.22.1", "alpine", "3.22.1")]
    [TestCase("ID=debian\nVERSION_ID='12'", "debian", "12")]
    [TestCase("ID=private-company\nVERSION_ID=123", "other", "unknown")]
    [TestCase("ID=arch\nVERSION_ID=rolling-private-host", "arch", "unknown")]
    [TestCase("ID=ubuntu\nVERSION_ID=$(hostname)", "ubuntu", "unknown")]
    [TestCase("ID=ubuntu\nID=alpine\nVERSION_ID=24.04", "unknown", "unknown")]
    [TestCase("PRETTY_NAME=private-host", "unknown", "unknown")]
    public void LinuxMetadataIsAllowlisted(string text, string distribution, string version) =>
        TelemetryRuntimeContextReader.ParseLinux(text).ShouldBe((distribution, version));

    /// <summary>Reports the installed tool release, removing branch/SHA metadata and rejecting private prerelease labels.</summary>
    [TestCase("7.0.0+Branch.private.Sha.123", "7.0.0")]
    [TestCase("7.1.0-beta.12+Branch.release.Sha.123", "7.1.0-beta.12")]
    [TestCase("7.1.0-rc.1", "7.1.0-rc.1")]
    [TestCase("7.1.0-private-customer.1", "unknown")]
    [TestCase("private/path", "unknown")]
    [TestCase(null, "unknown")]
    public void ToolVersionExcludesBuildIdentifiers(string? value, string expected) =>
        TelemetryRuntimeContextReader.NormalizeToolVersion(value).ShouldBe(expected);

    /// <summary>Distinguishes Docker from generic containers without conflating the OS userland with the host.</summary>
    [TestCase("docker", null, false, "docker")]
    [TestCase(null, "true", false, "other")]
    [TestCase(null, "1", false, "other")]
    [TestCase(null, null, true, "docker")]
    [TestCase("private", null, false, "none")]
    public void ContainerSignalsAreBounded(string? marker, string? dotnetHint, bool dockerFile, string expected)
    {
        var fs = Substitute.For<IFileSystem>();
        fs.File.Exists("/.dockerenv").Returns(dockerFile);
        var env = new TestEnvironment();
        env.SetEnvironmentVariable("GITVERSION_CONTAINER_KIND", marker);
        env.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", dotnetHint);
        var result = new TelemetryRuntimeContextReader(fs, env, "linux", new Version(1, 2)).Read();
        result.Container.ShouldBe(expected);
    }

    /// <summary>Reads the preferred Linux metadata file and falls back only when it is absent.</summary>
    [TestCase(true, "/etc/os-release", "ubuntu", "24.04")]
    [TestCase(false, "/usr/lib/os-release", "alpine", "3.22")]
    public void LinuxFilePrecedenceIsRespected(bool etcExists, string expectedPath, string distro, string release)
    {
        var fs = Substitute.For<IFileSystem>();
        fs.File.Exists("/etc/os-release").Returns(etcExists);
        fs.File.OpenText(expectedPath).Returns(_ => Reader($"ID={distro}\nVERSION_ID={release}"));
        var result = new TelemetryRuntimeContextReader(fs, new TestEnvironment(), "linux", new Version(1, 2)).Read();
        result.Distribution.ShouldBe(distro);
        result.OsVersion.ShouldBe(release);
        fs.File.Received(1).OpenText(expectedPath);
        fs.File.DidNotReceive().OpenText(etcExists ? "/usr/lib/os-release" : "/etc/os-release");
    }

    /// <summary>Rejects oversized metadata and does not fall back after an unreadable preferred file.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public void InvalidPreferredFileFailsClosed(bool unreadable)
    {
        var fs = Substitute.For<IFileSystem>();
        fs.File.Exists("/etc/os-release").Returns(true);
        fs.File.OpenText("/etc/os-release").Returns(_ => unreadable
            ? throw new IOException("private-path") : Reader("ID=ubuntu\nVERSION_ID=24.04\n" + new string('x', 8193)));
        var result = new TelemetryRuntimeContextReader(fs, new TestEnvironment(), "linux", new Version(1, 2)).Read();
        result.Distribution.ShouldBe("unknown");
        result.OsVersion.ShouldBe("unknown");
        fs.File.DidNotReceive().OpenText("/usr/lib/os-release");
    }

    /// <summary>Uses .NET's numeric OS version without reading release files or reporting revision identifiers.</summary>
    [TestCase("macos", 15, 7, 1, "15.7.1")]
    [TestCase("macos", 26, 0, -1, "26.0.0")]
    [TestCase("macos", 27, 2, 0, "27.2.0")]
    [TestCase("windows", 10, 0, 26100, "10.0.26100")]
    [TestCase("windows", 10, 0, -1, "10.0.0")]
    public void MacOsAndWindowsUseDotNetVersionWithoutReadingFiles(string os, int major, int minor, int build, string expected)
    {
        var fs = Substitute.For<IFileSystem>();
        var version = build < 0 ? new Version(major, minor) : new Version(major, minor, build, 1234);
        var result = new TelemetryRuntimeContextReader(fs, new TestEnvironment(), os, version).Read();
        result.Distribution.ShouldBe(os);
        result.OsVersion.ShouldBe(expected);
        fs.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Provides disposable in-memory file contents to the injected filesystem.</summary>
    private static StreamReader Reader(string value) => new(new MemoryStream(Encoding.UTF8.GetBytes(value)));
}
