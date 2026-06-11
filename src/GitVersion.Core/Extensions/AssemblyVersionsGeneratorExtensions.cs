using GitVersion.Configuration;

namespace GitVersion.Extensions;

/// <summary>Extension methods on <see cref="SemanticVersion"/> for generating assembly version strings.</summary>
public static class AssemblyVersionsGeneratorExtensions
{
    extension(SemanticVersion sv)
    {
        /// <summary>Returns the <c>AssemblyVersionAttribute</c> value for the given <paramref name="scheme"/>, or <see langword="null"/> when the scheme is <see cref="AssemblyVersioningScheme.None"/>.</summary>
        public string? GetAssemblyVersion(AssemblyVersioningScheme scheme) =>
            scheme switch
            {
                AssemblyVersioningScheme.Major => $"{sv.Major}.0.0.0",
                AssemblyVersioningScheme.MajorMinor => $"{sv.Major}.{sv.Minor}.0.0",
                AssemblyVersioningScheme.MajorMinorPatch => $"{sv.Major}.{sv.Minor}.{sv.Patch}.0",
                AssemblyVersioningScheme.MajorMinorPatchTag => $"{sv.Major}.{sv.Minor}.{sv.Patch}.{sv.PreReleaseTag.Number ?? 0}",
                AssemblyVersioningScheme.None => null,
                _ => throw new ArgumentException($"Unexpected value ({scheme}).", nameof(scheme))
            };

        /// <summary>Returns the <c>AssemblyFileVersionAttribute</c> value for the given <paramref name="scheme"/>, or <see langword="null"/> when the scheme is <see cref="AssemblyFileVersioningScheme.None"/>.</summary>
        public string? GetAssemblyFileVersion(AssemblyFileVersioningScheme scheme) =>
            scheme switch
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
