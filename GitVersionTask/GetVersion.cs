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
        public Stability? Stability { get; set; }

        [Output]
        public int? PreReleasePartTwo { get; set; }

        [Output]
        public int? PreReleasePartOne { get; set; }

        [Output]
        public string SemVer { get; set; }

        [Output]
        public string Sha { get; set; }

        [Output]
        public string BranchType { get; set; }

        [Output]
        public string BranchName { get; set; }
        [Output]
        public string MajorMinorPatch { get; set; }

        [Output]
        public string ShortVersion { get; set; }

        [Output]
        public string NugetVersion { get; set; }

        [Output]
        public string LongVersion { get; set; }

        [Output]
        public string Suffix { get; set; }

        [Output]
        public int Patch { get; set; }

        [Output]
        public int Minor { get; set; }

        [Output]
        public int Major { get; set; }

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
                VersionAndBranch versionAndBranch;
                if (VersionAndBranchFinder.TryGetVersion(SolutionDirectory, out versionAndBranch))
                {
                    MajorMinorPatch = string.Format("{0}.{1}.{2}", versionAndBranch.Version.Major, versionAndBranch.Version.Minor, versionAndBranch.Version.Patch);
                    Major = versionAndBranch.Version.Major;
                    Minor = versionAndBranch.Version.Minor;
                    Patch = versionAndBranch.Version.Patch;
                    Suffix = versionAndBranch.Version.Suffix;
                    LongVersion = versionAndBranch.ToLongString();
                    NugetVersion = versionAndBranch.GenerateNugetVersion();
                    ShortVersion = versionAndBranch.ToShortString();
                    BranchName = versionAndBranch.BranchName;
                    BranchType = versionAndBranch.BranchType == null ? null : versionAndBranch.BranchType.ToString();
                    Sha = versionAndBranch.Sha;
                    SemVer = versionAndBranch.GenerateSemVer();
                    var releaseInformation = ReleaseInformationCalculator.Calculate(versionAndBranch.BranchType, versionAndBranch.Version.Tag);
                    PreReleasePartOne = releaseInformation.ReleaseNumber;
                    PreReleasePartTwo = versionAndBranch.Version.PreReleasePartTwo;
                    Stability = releaseInformation.Stability;
                }
                return true;
            }
            catch (ErrorException errorException)
            {
                logger.LogError(errorException.Message);
                return false;
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