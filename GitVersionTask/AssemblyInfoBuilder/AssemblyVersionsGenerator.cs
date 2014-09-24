
    using System;
    using GitVersion;

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
            default:
                throw new ArgumentException(string.Format("Unexpected value ({0}).", scheme), "scheme");
        }

    }

}