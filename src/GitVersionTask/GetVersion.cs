namespace GitVersionTask
{
    public static class GetVersion
    {
        public static Output Execute(Input input) => GitVersionTasks.GetVersion(input);

        public sealed class Input : InputBase
        {
        }

        public sealed class Output
        {
            public string Major { get; set; }

            public string Minor { get; set; }

            public string Patch { get; set; }

            public string PreReleaseTag { get; set; }

            public string PreReleaseTagWithDash { get; set; }

            public string PreReleaseLabel { get; set; }

            public string PreReleaseNumber { get; set; }

            public string WeightedPreReleaseNumber { get; set; }

            public string BuildMetaData { get; set; }

            public string BuildMetaDataPadded { get; set; }

            public string FullBuildMetaData { get; set; }

            public string MajorMinorPatch { get; set; }

            public string SemVer { get; set; }

            public string LegacySemVer { get; set; }

            public string LegacySemVerPadded { get; set; }

            public string AssemblySemVer { get; set; }

            public string AssemblySemFileVer { get; set; }

            public string FullSemVer { get; set; }

            public string InformationalVersion { get; set; }

            public string BranchName { get; set; }

            public string Sha { get; set; }

            public string ShortSha { get; set; }

            public string NuGetVersionV2 { get; set; }

            public string NuGetVersion { get; set; }

            public string NuGetPreReleaseTagV2 { get; set; }

            public string NuGetPreReleaseTag { get; set; }

            public string CommitDate { get; set; }

            public string VersionSourceSha { get; set; }

            public string CommitsSinceVersionSource { get; set; }

            public string CommitsSinceVersionSourcePadded { get; set; }
        }
    }
}
