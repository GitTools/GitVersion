namespace GitFlowVersion
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LibGit2Sharp;

    abstract class OptionallyTaggedBranchVersionFinderBase
    {
        public VersionAndBranch FindVersion(
            IRepository repo,
            Branch branch,
            Commit commit,
            BranchType branchType,
            string baseBranchName)
        {
            var nbHotfixCommits = NumberOfCommitsInBranchNotKnownFromBaseBranch(repo, branch, branchType, baseBranchName);

            var versionString = branch.GetSuffix(branchType);
            var version = SemanticVersionParser.Parse(versionString, false);

            EnsureVersionIsValid(version, branch, branchType);

            var tagVersion = RetrieveMostRecentOptionalTagVersion(repo, version, branch.Commits.Take(nbHotfixCommits + 1));

            var versionAndBranch = new VersionAndBranch
                                   {
                                       BranchType = branchType,
                                       BranchName = branch.Name,
                                       Sha = commit.Sha,
                                       Version = new SemanticVersion
                                                 {
                                                     Major = version.Major,
                                                     Minor = version.Minor,
                                                     Patch = version.Patch,
                                                     Stability = version.Stability ?? Stability.Beta,
                                                     PreReleasePartOne = version.PreReleasePartOne ?? 0,
                                                     PreReleasePartTwo = (nbHotfixCommits == 0) ? default(int?) : nbHotfixCommits
                                                 },
                                   };

            if (tagVersion != null)
            {
                versionAndBranch.Version.Stability = tagVersion.Stability;
                versionAndBranch.Version.PreReleasePartOne = tagVersion.PreReleasePartOne;
            }

            return versionAndBranch;
        }

        SemanticVersion RetrieveMostRecentOptionalTagVersion(
            IRepository repository, SemanticVersion branchVersion, IEnumerable<Commit> take)
        {
            foreach (var commit in take)
            {
                foreach (var tag in repository.TagsByDate(commit))
                {
                    SemanticVersion version;
                    if (!SemanticVersionParser.TryParse(tag.Name, out version))
                    {
                        continue;
                    }

                    if (branchVersion.Major != version.Major ||
                        branchVersion.Minor != version.Minor ||
                        branchVersion.Patch != version.Patch)
                    {
                        continue;
                    }

                    return version;
                }
            }

            return null;
        }

        void EnsureVersionIsValid(SemanticVersion version, Branch branch, BranchType branchType)
        {
            var msg = string.Format("Branch '{0}' doesn't respect the {1} branch naming convention. ",
                branch.Name, branchType);

            if (version.Stability != null ||
                version.PreReleasePartOne != null ||
                version.PreReleasePartTwo != null)
            {
                throw new ErrorException(msg +
                                         string.Format("Supported format is '{0}-Major.Minor.Patch'.", branchType.ToString().ToLowerInvariant()));
            }

            switch (branchType)
            {
                case BranchType.Hotfix:
                    if (version.Patch == 0)
                    {
                        throw new ErrorException(msg + "A patch segment different than zero is required.");
                    }

                    break;

                case BranchType.Release:
                    if (version.Patch != 0)
                    {
                        throw new ErrorException(msg + "A patch segment equals to zero is required.");
                    }

                    break;

                default:
                    throw new NotSupportedException(string.Format("Unexpected branch type {0}.", branchType));
            }

            if (version.Stability == Stability.Final || version.Stability == null)
            {
                return;
            }

            if (version.PreReleasePartOne == null)
            {
                throw new ErrorException(msg +
                                         string.Format("When a stability is defined on a {0} branch the pre-release part one number must also be defined."
                                             , branchType));
            }
        }

        int NumberOfCommitsInBranchNotKnownFromBaseBranch(
            IRepository repo,
            Branch branch,
            BranchType branchType,
            string baseBranchName)
        {
            var baseTip = repo.Branches[baseBranchName].Tip;

            if (branch.Tip == baseTip)
            {
                // The branch bears no additional commit
                return 0;
            }

            var ancestor = repo.Commits.FindCommonAncestor(
                baseTip,
                branch.Tip);

            if (ancestor == null)
            {
                throw new ErrorException(
                    string.Format("A {0} branch is expected to branch off of '{1}'. "
                                  + "However, branch '{1}' and '{2}' do not share a common ancestor."
                        , branchType, baseBranchName, branch.Name));
            }

            var filter = new CommitFilter
                         {
                             Since = branch.Tip,
                             Until = ancestor

                         };

            return repo.Commits.QueryBy(filter).Count();
        }
    }
}
