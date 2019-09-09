using System;

namespace GitVersion.Extensions
{
    public static class AssemblyVersionsGenerator
    {
        public static string GetAssemblyVersion(
            this SemanticVersion sv,
            AssemblyVersioningScheme scheme)
        {
            switch (scheme)
            {
                case AssemblyVersioningScheme.Major:
                    return $"{sv.Major}.0.0.0";
                case AssemblyVersioningScheme.MajorMinor:
                    return $"{sv.Major}.{sv.Minor}.0.0";
                case AssemblyVersioningScheme.MajorMinorPatch:
                    return $"{sv.Major}.{sv.Minor}.{sv.Patch}.0";
                case AssemblyVersioningScheme.MajorMinorPatchTag:
                    return $"{sv.Major}.{sv.Minor}.{sv.Patch}.{sv.PreReleaseTag.Number ?? 0}";
                case AssemblyVersioningScheme.None:
                    return null;
                default:
                    throw new ArgumentException($"Unexpected value ({scheme}).", nameof(scheme));
            }
        }

        public static string GetAssemblyFileVersion(
        this SemanticVersion sv,
        AssemblyFileVersioningScheme scheme)
        {
            switch (scheme)
            {
                case AssemblyFileVersioningScheme.Major:
                    return $"{sv.Major}.0.0.0";
                case AssemblyFileVersioningScheme.MajorMinor:
                    return $"{sv.Major}.{sv.Minor}.0.0";
                case AssemblyFileVersioningScheme.MajorMinorPatch:
                    return $"{sv.Major}.{sv.Minor}.{sv.Patch}.0";
                case AssemblyFileVersioningScheme.MajorMinorPatchTag:
                    return $"{sv.Major}.{sv.Minor}.{sv.Patch}.{sv.PreReleaseTag.Number ?? 0}";
                case AssemblyFileVersioningScheme.None:
                    return null;
                default:
                    throw new ArgumentException($"Unexpected value ({scheme}).", nameof(scheme));
            }
        }
    }
}
