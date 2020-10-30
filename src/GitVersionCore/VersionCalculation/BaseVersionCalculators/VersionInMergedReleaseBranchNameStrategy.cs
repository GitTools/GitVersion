using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using GitVersion.Common;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Model.Configuration;

namespace GitVersion.VersionCalculation
{
    /// <summary>
    /// Version is extracted from the name of merged 'is release branch' when merged to master.
    /// BaseVersionSource is the current commit.
    /// Does not increment.
    /// </summary>
    public class VersionInMergedReleaseBranchNameStrategy : VersionStrategyBase
    {
        private readonly IRepositoryMetadataProvider repositoryMetadataProvider;

        public VersionInMergedReleaseBranchNameStrategy(IRepositoryMetadataProvider repositoryMetadataProvider, Lazy<GitVersionContext> versionContext) : base(versionContext)
        {
            this.repositoryMetadataProvider = repositoryMetadataProvider ?? throw new ArgumentNullException(nameof(repositoryMetadataProvider));
        }

        public override IEnumerable<BaseVersion> GetVersions()
        {
            var tagPrefixRegex = Context.Configuration.GitTagPrefix;

            var commit = Context.CurrentCommit;
            var branchName = Context.CurrentBranch.FriendlyName;
            var configuration = Context.FullConfiguration;

            var masterBranchRegex = configuration.Branches[Config.MasterBranchKey].Regex;

            // TODO : Should we check master + tracks release branches, should tracks release branches be set on master ?
            if (!Regex.IsMatch(branchName, masterBranchRegex, RegexOptions.IgnoreCase))
            {
                yield break;
            }

            // TODO : Is this the optimal way to find branches merged into current commit ?
            var parentBranch = commit.Parents.SelectMany(p =>
                repositoryMetadataProvider.GetBranchesContainingCommit(p).Where(b =>
                    configuration.IsReleaseBranch(b.FriendlyName))).Distinct().SingleOrDefault();
            if (parentBranch == null)
            {
                yield break;
            }

            branchName = parentBranch.FriendlyName;
            var versionInBranch = GetVersionInBranch(branchName, tagPrefixRegex);
            if (versionInBranch != null)
            {
                // TODO : Not certain what this branch name override is about ...
                var branchNameOverride = branchName.RegexReplace("[-/]" + versionInBranch.Item1, string.Empty);
                yield return new BaseVersion("Version in parent branch name", false, versionInBranch.Item2, commit, branchNameOverride);
            }
        }

        private Tuple<string, SemanticVersion> GetVersionInBranch(string branchName, string tagPrefixRegex)
        {
            var branchParts = branchName.Split('/', '-');
            foreach (var part in branchParts)
            {
                if (SemanticVersion.TryParse(part, tagPrefixRegex, out var semanticVersion))
                {
                    return Tuple.Create(part, semanticVersion);
                }
            }

            return null;
        }
    }
}
