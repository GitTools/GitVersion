using System;

namespace GitVersion.Extensions
{
    public enum AssemblyFileVersioningScheme
    {
        MajorMinorPatchTag,
        MajorMinorPatch,
        MajorMinor,
        Major,
        None
    }

    public enum AssemblyVersioningScheme
    {
        MajorMinorPatchTag,
        MajorMinorPatch,
        MajorMinor,
        Major,
        None
    }

    public static class AssemblyVersionsGeneratorExtensions
    {
        public static string GetAssemblyVersion(this SemanticVersion sv, AssemblyVersioningScheme scheme)
        {
            return scheme switch
            {
                AssemblyVersioningScheme.Major => $"{sv.Major}.0.0.0",
                AssemblyVersioningScheme.MajorMinor => $"{sv.Major}.{sv.Minor}.0.0",
                AssemblyVersioningScheme.MajorMinorPatch => $"{sv.Major}.{sv.Minor}.{sv.Patch}.0",
                AssemblyVersioningScheme.MajorMinorPatchTag => $"{sv.Major}.{sv.Minor}.{sv.Patch}.{sv.PreReleaseTag.Number ?? 0}",
                AssemblyVersioningScheme.None => null,
                _ => throw new ArgumentException($"Unexpected value ({scheme}).", nameof(scheme))
            };
        }

        public static string GetAssemblyFileVersion(this SemanticVersion sv, AssemblyFileVersioningScheme scheme)
        {
            return scheme switch
            {
                AssemblyFileVersioningScheme.Major => $"{sv.Major}.0.0.0",
                AssemblyFileVersioningScheme.MajorMinor => $"{sv.Major}.{sv.Minor}.0.0",
                AssemblyFileVersioningScheme.MajorMinorPatch => $"{sv.Major}.{sv.Minor}.{sv.Patch}.0",
                AssemblyFileVersioningScheme.MajorMinorPatchTag => $"{sv.Major}.{sv.Minor}.{sv.Patch}.{sv.PreReleaseTag.Number ?? 0}",
                AssemblyFileVersioningScheme.None => null,
                _ => throw new ArgumentException($"Unexpected value ({scheme}).", nameof(scheme))
            };
        }
    }
}
