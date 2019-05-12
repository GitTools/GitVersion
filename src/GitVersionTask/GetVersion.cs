namespace GitVersionTask
{
    using System;

    public static class GetVersion
    {

        public static Output Execute(
            Input input
            )
        {
            return GitVersionTaskBase.ExecuteGitVersionTask(
                input,
                InnerExecute
                );
        }

        private static Output InnerExecute(
            Input input,
            TaskLogger logger
            )
        {
            if (!GitVersionTaskBase.CreateExecuteCore().TryGetVersion(input.SolutionDirectory, out var versionVariables, input.NoFetch, new GitVersion.Authentication()))
            {
                return null;
            }

            var outputType = typeof(Output);
            var output = new Output();
            foreach (var variable in versionVariables)
            {
                outputType.GetProperty(variable.Key).SetValue(output, variable.Value, null);
            }

            return output;
        }

        public sealed class Input : GitVersionTaskBase.AbstractInput
        {

        }

        public sealed class Output
        {
            public String Major { get; set; }

            public String Minor { get; set; }

            public String Patch { get; set; }

            public String PreReleaseTag { get; set; }

            public String PreReleaseTagWithDash { get; set; }

            public String PreReleaseLabel { get; set; }

            public String PreReleaseNumber { get; set; }

            public String WeightedPreReleaseNumber { get; set; }

            public String BuildMetaData { get; set; }

            public String BuildMetaDataPadded { get; set; }

            public String FullBuildMetaData { get; set; }

            public String MajorMinorPatch { get; set; }

            public String SemVer { get; set; }

            public String LegacySemVer { get; set; }

            public String LegacySemVerPadded { get; set; }

            public String AssemblySemVer { get; set; }

            public String AssemblySemFileVer { get; set; }

            public String FullSemVer { get; set; }

            public String InformationalVersion { get; set; }

            public String BranchName { get; set; }

            public String Sha { get; set; }

            public String ShortSha { get; set; }

            public String NuGetVersionV2 { get; set; }

            public String NuGetVersion { get; set; }

            public String NuGetPreReleaseTagV2 { get; set; }

            public String NuGetPreReleaseTag { get; set; }

            public String CommitDate { get; set; }

            public String VersionSourceSha { get; set; }

            public String CommitsSinceVersionSource { get; set; }

            public String CommitsSinceVersionSourcePadded { get; set; }
        }

    }
}
