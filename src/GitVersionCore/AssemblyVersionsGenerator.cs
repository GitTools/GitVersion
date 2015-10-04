namespace GitVersion
{
    using System;

    public static class AssemblyVersionsGenerator
    {
        public static string GetAssemblyVersion(
            this SemanticVersion sv,
            AssemblyVersioningScheme scheme)
        {
            switch (scheme)
            {
                case AssemblyVersioningScheme.Major:
                    return string.Format("{0}.0.0.0", sv.Major);
                case AssemblyVersioningScheme.MajorMinor:
                    return string.Format("{0}.{1}.0.0", sv.Major, sv.Minor);
                case AssemblyVersioningScheme.MajorMinorPatch:
                    return string.Format("{0}.{1}.{2}.0", sv.Major, sv.Minor, sv.Patch);
                case AssemblyVersioningScheme.MajorMinorPatchTag:
                    return string.Format("{0}.{1}.{2}.{3}", sv.Major, sv.Minor, sv.Patch, sv.PreReleaseTag.Number ?? 0);
                default:
                    throw new ArgumentException(string.Format("Unexpected value ({0}).", scheme), "scheme");
            }
        }

        public static string GetAssemblyInformationalVersion(
            this SemanticVersion sv,
            AssemblyInformationalVersioningScheme scheme)
        {
            switch (scheme)
            {
                case AssemblyInformationalVersioningScheme.FullInformationalVersion:
                    return sv.ToString("i");
                case AssemblyInformationalVersioningScheme.NugetVersion:
                    return sv.ToString("lp");
                default:
                    throw new ArgumentException(string.Format("Unexpected value ({0}).", scheme), "scheme");
            }
        }

    }
}