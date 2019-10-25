// This code originally copied from https://raw.githubusercontent.com/dotnet/sourcelink/master/src/Microsoft.Build.Tasks.Git/RuntimeIdMap.cs
#if !NETFRAMEWORK
using System;
using System.Diagnostics;

namespace GitVersion.MSBuildTask.LibGit2Sharp
{
    internal static class RuntimeIdMap
    {
        // This functionality needs to be provided as .NET Core API.
        // Releated issues:
        // https://github.com/dotnet/core-setup/issues/1846
        // https://github.com/NuGet/Home/issues/5862

        public static string GetNativeLibraryDirectoryName(string runtimeIdentifier)
        {
#if DEBUG
            Debug.Assert(SDirectories.Length == SRids.Length);

            for (var i = 1; i < SRids.Length; i++)
            {
                Debug.Assert(StringComparer.Ordinal.Compare(SRids[i - 1], SRids[i]) < 0);
            }
#endif
            var index = Array.BinarySearch(SRids, runtimeIdentifier, StringComparer.Ordinal);
            if (index < 0)
            {
                // Take the runtime id with highest version of matching OS.
                // The runtimes in the table are currently sorted so that this works.

                ParseRuntimeId(runtimeIdentifier, out var runtimeOs, out var runtimeVersion, out var runtimeQualifiers);

                // find version-less rid:
                var bestMatchIndex = -1;
                string[] bestVersion = null;

                void FindBestCandidate(int startIndex, int increment)
                {
                    var i = startIndex;
                    while (i >= 0 && i < SRids.Length)
                    {
                        var candidate = SRids[i];
                        ParseRuntimeId(candidate, out var candidateOs, out var candidateVersion, out var candidateQualifiers);
                        if (candidateOs != runtimeOs)
                        {
                            break;
                        }

                        // Find the highest available version that is lower than or equal to the runtime version
                        // among candidates that have the same qualifiers.
                        if (candidateQualifiers == runtimeQualifiers &&
                            CompareVersions(candidateVersion, runtimeVersion) <= 0 &&
                            (bestVersion == null || CompareVersions(candidateVersion, bestVersion) > 0))
                        {
                            bestMatchIndex = i;
                            bestVersion = candidateVersion;
                        }

                        i += increment;
                    }
                }

                FindBestCandidate(~index - 1, -1);
                FindBestCandidate(~index, +1);

                if (bestMatchIndex < 0)
                {
                    throw new PlatformNotSupportedException(runtimeIdentifier);
                }

                index = bestMatchIndex;
            }

            return SDirectories[index];
        }

        internal static int CompareVersions(string[] left, string[] right)
        {
            for (var i = 0; i < Math.Max(left.Length, right.Length); i++)
            {
                // pad with zeros (consider "1.2" == "1.2.0")
                var leftPart = (i < left.Length) ? left[i] : "0";
                var rightPart = (i < right.Length) ? right[i] : "0";

                int result;
                if (!int.TryParse(leftPart, out var leftNumber) || !int.TryParse(rightPart, out var rightNumber))
                {
                    // alphabetical order:
                    result = StringComparer.Ordinal.Compare(leftPart, rightPart);
                }
                else
                {
                    // numerical order:
                    result = leftNumber.CompareTo(rightNumber);
                }

                if (result != 0)
                {
                    return result;
                }
            }

            return 0;
        }

        internal static void ParseRuntimeId(string runtimeId, out string osName, out string[] version, out string qualifiers)
        {
            // We use the following convention in all newly-defined RIDs. Some RIDs (win7-x64, win8-x64) predate this convention and don't follow it, but all new RIDs should follow it.
            // [os name].[version]-[architecture]-[additional qualifiers]
            // See https://github.com/dotnet/corefx/blob/master/pkg/Microsoft.NETCore.Platforms/readme.md#naming-convention

            var versionSeparator = runtimeId.IndexOf('.');
            if (versionSeparator >= 0)
            {
                osName = runtimeId.Substring(0, versionSeparator);
            }
            else
            {
                osName = null;
            }

            var architectureSeparator = runtimeId.IndexOf('-', versionSeparator + 1);
            if (architectureSeparator >= 0)
            {
                if (versionSeparator >= 0)
                {
                    version = runtimeId.Substring(versionSeparator + 1, architectureSeparator - versionSeparator - 1).Split('.');
                }
                else
                {
                    osName = runtimeId.Substring(0, architectureSeparator);
                    version = Array.Empty<string>();
                }

                qualifiers = runtimeId.Substring(architectureSeparator + 1);
            }
            else
            {
                if (versionSeparator >= 0)
                {
                    version = runtimeId.Substring(versionSeparator + 1).Split('.');
                }
                else
                {
                    osName = runtimeId;
                    version = Array.Empty<string>();
                }

                qualifiers = string.Empty;
            }
        }

        // The following tables were generated by scripts/RuntimeIdMapGenerator.csx.
        // Regenerate when upgrading LibGit2Sharp to a new version that supports more platforms.

        private static readonly string[] SRids = new[]
        {
            "alpine-x64",
            "alpine.3.6-x64",
            "alpine.3.7-x64",
            "alpine.3.8-x64",
            "alpine.3.9-x64",
            "centos-x64",
            "centos.7-x64",
            "debian-x64",
            "debian.8-x64",
            "debian.9-x64",
            "fedora-x64",
            "fedora.23-x64",
            "fedora.24-x64",
            "fedora.25-x64",
            "fedora.26-x64",
            "fedora.27-x64",
            "fedora.28-x64",
            "fedora.29-x64",
            "gentoo-x64",
            "linux-musl-x64",
            "linux-x64",
            "linuxmint.17-x64",
            "linuxmint.17.1-x64",
            "linuxmint.17.2-x64",
            "linuxmint.17.3-x64",
            "linuxmint.18-x64",
            "linuxmint.18.1-x64",
            "linuxmint.18.2-x64",
            "linuxmint.18.3-x64",
            "linuxmint.19-x64",
            "ol-x64",
            "ol.7-x64",
            "ol.7.0-x64",
            "ol.7.1-x64",
            "ol.7.2-x64",
            "ol.7.3-x64",
            "ol.7.4-x64",
            "ol.7.5-x64",
            "ol.7.6-x64",
            "opensuse-x64",
            "opensuse.13.2-x64",
            "opensuse.15.0-x64",
            "opensuse.42.1-x64",
            "opensuse.42.2-x64",
            "opensuse.42.3-x64",
            "osx",
            "osx-x64",
            "osx.10.10",
            "osx.10.10-x64",
            "osx.10.11",
            "osx.10.11-x64",
            "osx.10.12",
            "osx.10.12-x64",
            "osx.10.13",
            "osx.10.13-x64",
            "osx.10.14",
            "osx.10.14-x64",
            "rhel-x64",
            "rhel.6-x64",
            "rhel.7-x64",
            "rhel.7.0-x64",
            "rhel.7.1-x64",
            "rhel.7.2-x64",
            "rhel.7.3-x64",
            "rhel.7.4-x64",
            "rhel.7.5-x64",
            "rhel.7.6-x64",
            "rhel.8-x64",
            "rhel.8.0-x64",
            "sles-x64",
            "sles.12-x64",
            "sles.12.1-x64",
            "sles.12.2-x64",
            "sles.12.3-x64",
            "sles.15-x64",
            "ubuntu-x64",
            "ubuntu.14.04-x64",
            "ubuntu.14.10-x64",
            "ubuntu.15.04-x64",
            "ubuntu.15.10-x64",
            "ubuntu.16.04-x64",
            "ubuntu.16.10-x64",
            "ubuntu.17.04-x64",
            "ubuntu.17.10-x64",
            "ubuntu.18.04-x64",
            "ubuntu.18.10-x64",
            "win-x64",
            "win-x64-aot",
            "win-x86",
            "win-x86-aot",
            "win10-x64",
            "win10-x64-aot",
            "win10-x86",
            "win10-x86-aot",
            "win7-x64",
            "win7-x64-aot",
            "win7-x86",
            "win7-x86-aot",
            "win8-x64",
            "win8-x64-aot",
            "win8-x86",
            "win8-x86-aot",
            "win81-x64",
            "win81-x64-aot",
            "win81-x86",
            "win81-x86-aot",
        };

        private static readonly string[] SDirectories = new[]
        {
            "alpine-x64",
            "alpine-x64",
            "alpine-x64",
            "alpine-x64",
            "alpine-x64",
            "rhel-x64",
            "rhel-x64",
            "linux-x64",
            "linux-x64",
            "debian.9-x64",
            "fedora-x64",
            "fedora-x64",
            "fedora-x64",
            "fedora-x64",
            "fedora-x64",
            "fedora-x64",
            "fedora-x64",
            "fedora-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "ubuntu.18.04-x64",
            "rhel-x64",
            "rhel-x64",
            "rhel-x64",
            "rhel-x64",
            "rhel-x64",
            "rhel-x64",
            "rhel-x64",
            "rhel-x64",
            "rhel-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "osx",
            "osx",
            "osx",
            "osx",
            "osx",
            "osx",
            "osx",
            "osx",
            "osx",
            "osx",
            "osx",
            "osx",
            "rhel-x64",
            "rhel-x64",
            "rhel-x64",
            "rhel-x64",
            "rhel-x64",
            "rhel-x64",
            "rhel-x64",
            "rhel-x64",
            "rhel-x64",
            "rhel-x64",
            "rhel-x64",
            "rhel-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "linux-x64",
            "ubuntu.18.04-x64",
            "linux-x64",
            "win-x64",
            "win-x64",
            "win-x86",
            "win-x86",
            "win-x64",
            "win-x64",
            "win-x86",
            "win-x86",
            "win-x64",
            "win-x64",
            "win-x86",
            "win-x86",
            "win-x64",
            "win-x64",
            "win-x86",
            "win-x86",
            "win-x64",
            "win-x64",
            "win-x86",
            "win-x86",
        };
    }
}
#endif
