namespace GitVersion
{
    using System.IO;
    using GitVersion.VersionCalculation;
    using GitTools;

    public class GitVersionFinder
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            Logger.WriteInfo(string.Format(
                "Running against branch: {0} ({1})",
                context.CurrentBranch.FriendlyName,
                context.CurrentCommit == null ? "-" : context.CurrentCommit.Sha));
            if (context.IsCurrentCommitTagged)
            {
                Logger.WriteInfo($"Current commit is tagged with version {context.CurrentCommitTaggedVersion}, " +
                                 "version calcuation is for metadata only.");
            }
            EnsureMainTopologyConstraints(context);

            var filePath = Path.Combine(context.Repository.GetRepositoryDirectory(), "NextVersion.txt");
            if (File.Exists(filePath))
            {            
                throw new GitTools.WarningException("NextVersion.txt has been deprecated. See http://gitversion.readthedocs.org/en/latest/configuration/ for replacement");
            }

            return new NextVersionCalculator().FindVersion(context);
        }

        void EnsureMainTopologyConstraints(GitVersionContext context)
        {
            EnsureHeadIsNotDetached(context);
        }

        void EnsureHeadIsNotDetached(GitVersionContext context)
        {
            if (!context.CurrentBranch.IsDetachedHead())
            {
                return;
            }

            var message = string.Format(
                "It looks like the branch being examined is a detached Head pointing to commit '{0}'. " +
                "Without a proper branch name GitVersion cannot determine the build version.",
                context.CurrentCommit.Id.ToString(7));
            throw new WarningException(message);
        }
    }
}