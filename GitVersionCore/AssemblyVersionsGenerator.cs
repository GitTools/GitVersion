namespace GitVersion
{
    using System;

    public static class AssemblyVersionsGenerator
    {
        public static AssemblyMetaData Process(
            SemanticVersion sv,
            AssemblyVersioningScheme scheme,
            bool addNumberOfCommitsSinceTagOnMasterBranchToFileVersion)
        {
            var fileVersion = GetStrictAssemblyFileVersion(sv, addNumberOfCommitsSinceTagOnMasterBranchToFileVersion);
            string version;

            switch (scheme)
            {
                case AssemblyVersioningScheme.None:
                    version = "1.0.0.0";
                    break;

                case AssemblyVersioningScheme.Major:
                    version = string.Format("{0}.0.0.0", sv.Major);
                    break;

                case AssemblyVersioningScheme.MajorMinor:
                    version = string.Format("{0}.{1}.0.0", sv.Major, sv.Minor);
                    break;

                case AssemblyVersioningScheme.MajorMinorPatch:
                    version = GetStrictAssemblyFileVersion(sv, false);
                    break;

                default:
                    throw new ArgumentException(string.Format("Unexpected value ({0}).", scheme), "scheme");
            }

            return new AssemblyMetaData(version, fileVersion);
        }

        static string GetStrictAssemblyFileVersion(SemanticVersion sv, bool appendRevision)
        {
            if (appendRevision && sv.BuildMetaData.Branch == "master")
            {
                if (sv.BuildMetaData.CommitsSinceTag != null)
                {
                    return string.Format("{0}.{1}.{2}.{3}", sv.Major, sv.Minor, sv.Patch, sv.BuildMetaData.CommitsSinceTag);
                }
            }

            return string.Format("{0}.{1}.{2}.0", sv.Major, sv.Minor, sv.Patch);
        }
    }
}
