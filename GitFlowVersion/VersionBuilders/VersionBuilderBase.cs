namespace GitFlowVersion.VersionBuilders
{
    using System;

    public abstract class VersionBuilderBase : IVersionBuilder
    {
        public abstract string GenerateBuildVersion(VersionAndBranch versionAndBranch);

        public virtual string CreateVersionString(VersionAndBranch versionAndBranch)
        {
            var prereleaseString = string.Empty;

            var stability = versionAndBranch.Version.Stability;
            if (stability == null)
            {
                throw new Exception("Stability cannot be null");
            }
            if (stability != Stability.Final)
            {
                var preReleaseVersion = versionAndBranch.Version.PreReleasePartOne.ToString();
                if (versionAndBranch.Version.PreReleasePartTwo != null)
                {
                    preReleaseVersion += "." + versionAndBranch.Version.PreReleasePartTwo;
                }

                switch (versionAndBranch.BranchType)
                {
                    case BranchType.Develop:
                        prereleaseString = "-" + stability + preReleaseVersion;
                        break;

                    case BranchType.Release:
                        prereleaseString = "-" + stability + preReleaseVersion;
                        break;

                    case BranchType.Hotfix:
                        prereleaseString = "-" + stability + preReleaseVersion;
                        break;
                    case BranchType.PullRequest:
                        prereleaseString = "-PullRequest-" + versionAndBranch.Version.Suffix;
                        break;
                    case BranchType.Feature:
                        prereleaseString = "-Feature-" + versionAndBranch.BranchName + "-" + versionAndBranch.Sha;
                        break;
                }
            }
            return string.Format("{0}.{1}.{2}{3}", versionAndBranch.Version.Major, versionAndBranch.Version.Minor,
                versionAndBranch.Version.Patch, prereleaseString);
        }
    }
}
