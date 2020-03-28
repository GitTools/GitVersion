// This code originally copied from https://raw.githubusercontent.com/dotnet/sourcelink/master/src/Microsoft.Build.Tasks.Git/RuntimeIdMap.cs
#if !NETFRAMEWORK
using System;
using System.Diagnostics;
using Microsoft.DotNet.PlatformAbstractions;

namespace GitVersion.MSBuildTask.LibGit2Sharp
{
    internal static partial class RuntimeIdMap
    {
        // This functionality needs to be provided as .NET Core API.
        // Releated issues:
        // https://github.com/dotnet/core-setup/issues/1846
        // https://github.com/NuGet/Home/issues/5862

        public static string GetNativeLibraryDirectoryName()
        {
            var runtimeIdentifier = RuntimeEnvironment.GetRuntimeIdentifier();
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

        private static int CompareVersions(string[] left, string[] right)
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

        private static void ParseRuntimeId(string runtimeId, out string osName, out string[] version, out string qualifiers)
        {
            // We use the following convention in all newly-defined RIDs. Some RIDs (win7-x64, win8-x64) predate this convention and don't follow it, but all new RIDs should follow it.
            // [os name].[version]-[architecture]-[additional qualifiers]
            // See https://github.com/dotnet/corefx/blob/master/pkg/Microsoft.NETCore.Platforms/readme.md#naming-convention

            var versionSeparator = runtimeId.IndexOf('.');
            osName = versionSeparator >= 0 ? runtimeId.Substring(0, versionSeparator) : null;

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
    }


}
#endif
