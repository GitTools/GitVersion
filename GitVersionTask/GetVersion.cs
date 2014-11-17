namespace GitVersionTask
{
    using System;
    using GitVersion;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using Logger = GitVersion.Logger;

    public class GetVersion : Task
    {
        [Required]
        public string SolutionDirectory { get; set; }

        [Output]
        public string Major { get; set; }

        [Output]
        public string Minor { get; set; }

        [Output]
        public string Patch { get; set; }

        [Output]
        public string PreReleaseTag { get; set; }

        [Output]
        public string PreReleaseTagWithDash { get; set; }

        [Output]
        public string BuildMetaData { get; set; }

        [Output]
        public string FullBuildMetaData { get; set; }

        [Output]
        public string MajorMinorPatch { get; set; }

        [Output]
        public string SemVer { get; set; }

        [Output]
        public string LegacySemVer { get; set; }

        [Output]
        public string LegacySemVerPadded { get; set; }

        [Output]
        public string AssemblySemVer { get; set; }

        [Output]
        public string AssemblyFileSemVer { get; set; }

        [Output]
        public string FullSemVer { get; set; }

        [Output]
        public string InformationalVersion { get; set; }

        [Output]
        public string ClassicVersion { get; set; }

        [Output]
        public string ClassicVersionWithTag { get; set; }

        [Output]
        public string BranchName { get; set; }

        [Output]
        public string Sha { get; set; }

        [Output]
        public string NuGetVersionV2 { get; set; }

        [Output]
        public string NuGetVersion { get; set; }

        public string DevelopBranchTag { get; set; }

        public string ReleaseBranchTag { get; set; }

        public string TagPrefix { get; set; }

        public string NextVersion { get; set; }

        TaskLogger logger;

        public GetVersion()
        {
            logger = new TaskLogger(this);
            Logger.WriteInfo = this.LogInfo;
            Logger.WriteWarning = this.LogWarning;
        }

        public override bool Execute()
        {
            try
            {
                CachedVersion versionAndBranch;
                var gitDirectory = GitDirFinder.TreeWalkForGitDir(SolutionDirectory);
                var configuration = ConfigurationProvider.Provide(gitDirectory);

                // TODO This should be covered by tests
                // Null is intentional. Empty string means the user has set the value to an empty string and wants to clear the tag
                if (DevelopBranchTag != null)
                {
                    configuration.DevelopBranchTag = DevelopBranchTag;
                }

                if (ReleaseBranchTag != null)
                {
                    configuration.ReleaseBranchTag = ReleaseBranchTag;
                }

                if (TagPrefix != null)
                {
                    configuration.TagPrefix = TagPrefix;
                }

                if (NextVersion != null)
                {
                    configuration.NextVersion = NextVersion;
                }

                if (VersionAndBranchFinder.TryGetVersion(SolutionDirectory, out versionAndBranch, configuration))
                {
                    var thisType = typeof(GetVersion);
                    var variables = VariableProvider.GetVariablesFor(versionAndBranch.SemanticVersion, configuration);
                    foreach (var variable in variables)
                    {
                        thisType.GetProperty(variable.Key).SetValue(this, variable.Value, null);
                    }
                }
                return true;
            }
            catch (WarningException errorException)
            {
                logger.LogWarning(errorException.Message);
                return true;
            }
            catch (Exception exception)
            {
                logger.LogError("Error occurred: " + exception);
                return false;
            }
            finally
            {
                Logger.Reset();
            }
        }
    }
}