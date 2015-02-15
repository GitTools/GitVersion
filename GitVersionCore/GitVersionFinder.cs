namespace GitVersion
{
    using System;
    using System.IO;
    using System.Linq;
    using GitVersion.VersionCalculation;
    using LibGit2Sharp;

    public class GitVersionFinder
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            Logger.WriteInfo("Running against branch: " + context.CurrentBranch.Name);
            EnsureMainTopologyConstraints(context);

            var filePath = Path.Combine(context.Repository.GetRepositoryDirectory(), "NextVersion.txt");
            if (File.Exists(filePath))
            {
                throw new Exception("NextVersion.txt has been depreciated. See https://github.com/ParticularLabs/GitVersion/wiki/GitVersionConfig.yaml-Configuration-File for replacement");
            }

            return new NextVersionCalculator().FindVersion(context);
        }

        void EnsureMainTopologyConstraints(GitVersionContext context)
        {
            EnsureLocalBranchExists(context.Repository, "master");
            // TODO somehow enforce this? EnsureLocalBranchExists(context.Repository, "develop");
            EnsureHeadIsNotDetached(context);
        }

        void EnsureHeadIsNotDetached(GitVersionContext context)
        {
            if (!context.CurrentBranch.IsDetachedHead())
            {
                return;
            }

            var message = string.Format("It looks like the branch being examined is a detached Head pointing to commit '{0}'. Without a proper branch name GitVersion cannot determine the build version.", context.CurrentCommit.Id.ToString(7));
            throw new WarningException(message);
        }

        void EnsureLocalBranchExists(IRepository repository, string branchName)
        {
            if (repository.FindBranch(branchName) != null)
            {
                return;
            }

            var existingBranches = string.Format("'{0}'", string.Join("', '", repository.Branches.Select(x => x.CanonicalName)));
            throw new WarningException(string.Format("This repository doesn't contain a branch named '{0}'. Please create one. Existing branches: {1}", branchName, existingBranches));
        }
    }
}