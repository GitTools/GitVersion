namespace GitVersion
{
    public static class SemVerVersionBuilder
    {
        // TODO move to ToString on SemVer class
        public static string GenerateSemVer(this VersionAndBranch versionAndBranch)
        {
            if (versionAndBranch.Version.PreReleaseTag.HasTag())
            {
                return string.Format("{0}.{1}.{2}-{3}",
                    versionAndBranch.Version.Major,
                    versionAndBranch.Version.Minor,
                    versionAndBranch.Version.Patch,
                    versionAndBranch.Version.PreReleaseTag);
            }

            return string.Format("{0}.{1}.{2}",
                    versionAndBranch.Version.Major,
                    versionAndBranch.Version.Minor,
                    versionAndBranch.Version.Patch);
        }

        // TODO move to ToString on SemVer class
        public static string GenerateAssemblySemVer(this VersionAndBranch versionAndBranch)
        {
            return string.Format("{0}.{1}.{2}.0",
                    versionAndBranch.Version.Major,
                    versionAndBranch.Version.Minor,
                    versionAndBranch.Version.Patch);
        }

        // TODO move to ToString on SemVer class
        public static string GenerateClassicVersion(this VersionAndBranch versionAndBranch)
        {
            if (versionAndBranch.Version.PreReleaseTag.HasTag())
            {
                return string.Format("{0}.{1}.{2}.{3}",
                    versionAndBranch.Version.Major,
                    versionAndBranch.Version.Minor,
                    versionAndBranch.Version.Patch,
                    versionAndBranch.Version.PreReleaseTag.Number ?? 0);
            }

            return string.Format("{0}.{1}.{2}.0",
                    versionAndBranch.Version.Major,
                    versionAndBranch.Version.Minor,
                    versionAndBranch.Version.Patch);
        }

        // TODO move to ToString on SemVer class
        public static string GeneratePaddedSemVer(this VersionAndBranch versionAndBranch)
        {
            //TODO Fix Padded SemVer
            if (versionAndBranch.Version.PreReleaseTag.HasTag())
            {
                return string.Format("{0}.{1}.{2}-{3}",
                    versionAndBranch.Version.Major,
                    versionAndBranch.Version.Minor,
                    versionAndBranch.Version.Patch,
                    versionAndBranch.Version.PreReleaseTag.ToString("p"));
            }

            return string.Format("{0}.{1}.{2}",
                    versionAndBranch.Version.Major,
                    versionAndBranch.Version.Minor,
                    versionAndBranch.Version.Patch);
        }

        // TODO move to ToString on SemVer class
        public static string GenerateFullSemVer(this VersionAndBranch versionAndBranch)
        {
            if (versionAndBranch.Version.PreReleaseTag.HasTag())
            {
                return string.Format("{0}.{1}.{2}-{3}",
                    versionAndBranch.Version.Major,
                    versionAndBranch.Version.Minor,
                    versionAndBranch.Version.Patch,
                    versionAndBranch.Version.PreReleaseTag);
            }

            return string.Format("{0}.{1}.{2}",
                    versionAndBranch.Version.Major,
                    versionAndBranch.Version.Minor,
                    versionAndBranch.Version.Patch);
        }

        // TODO move to ToString on SemVer class
        public static string GeneratePaddedFullSemVer(this VersionAndBranch versionAndBranch)
        {
            //TODO Fix Padded SemVer
            if (versionAndBranch.Version.PreReleaseTag.HasTag() && !string.IsNullOrEmpty(versionAndBranch.Version.Suffix))
            {
                return string.Format("{0}.{1}.{2}-{3}+{4}",
                    versionAndBranch.Version.Major,
                    versionAndBranch.Version.Minor,
                    versionAndBranch.Version.Patch,
                    versionAndBranch.Version.PreReleaseTag.ToString("p"),
                    versionAndBranch.Version.Suffix);
            }
            if (versionAndBranch.Version.PreReleaseTag.HasTag())
            {
                return string.Format("{0}.{1}.{2}-{3}",
                    versionAndBranch.Version.Major,
                    versionAndBranch.Version.Minor,
                    versionAndBranch.Version.Patch,
                    versionAndBranch.Version.PreReleaseTag.ToString("p"));
            }
            if (!string.IsNullOrEmpty(versionAndBranch.Version.Suffix))
            {
                return string.Format("{0}.{1}.{2}+{3}",
                    versionAndBranch.Version.Major,
                    versionAndBranch.Version.Minor,
                    versionAndBranch.Version.Patch,
                    versionAndBranch.Version.Suffix);
            }

            return string.Format("{0}.{1}.{2}",
                    versionAndBranch.Version.Major,
                    versionAndBranch.Version.Minor,
                    versionAndBranch.Version.Patch);
        }
    }
}
