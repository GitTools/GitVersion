namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using LibGit2Sharp;

    /// <summary>
    /// Version is extracted from older commits's merge messages.
    /// BaseVersionSource is the commit where the message was found.
    /// Increments if PreventIncrementForMergedBranchVersion (from the branch config) is false.
    /// </summary>
    public class MergeMessageBaseVersionStrategy : BaseVersionStrategy
    {
        public override IEnumerable<BaseVersion> GetVersions(GitVersionContext context)
        {
            foreach (var config in context.Configurations)
            {
                var baseVersions = config.CurrentBranchInfo.RelevantCommits.Skip(1)
                    .SelectMany(commit =>
                    {
                        SemanticVersion semanticVersion;
                        if (TryParse(commit, config, out semanticVersion))
                        {
                            var shouldIncrement = !config.PreventIncrementForMergedBranchVersion;
                            return new[]
                            {
                                new BaseVersion(string.Format("Merge message '{0}'", commit.Message.Trim()), shouldIncrement, semanticVersion, commit, null)
                            };
                        }
                        return Enumerable.Empty<BaseVersion>();
                    }).ToList();

                foreach (var version in baseVersions)
                {
                    yield return version;
                }
            }
        }

        static bool TryParse(Commit mergeCommit, EffectiveConfiguration configuration, out SemanticVersion semanticVersion)
        {
            semanticVersion = ExtractVersionFromMessage(mergeCommit, configuration);
            return semanticVersion != null;
        }

        private static SemanticVersion ExtractVersionFromMessage(Commit mergeCommit, EffectiveConfiguration configuration)
        {
            if (mergeCommit.Parents.Count() < 2)
            {
                return null;
            }

            var commitMessage = mergeCommit.Message;
            var lastIndexOf = commitMessage.LastIndexOf("into", StringComparison.OrdinalIgnoreCase);
            if (lastIndexOf != -1)
                commitMessage = commitMessage.Substring(0, lastIndexOf);

            //TODO: Make the version prefixes customizable
            var possibleVersions = Regex.Matches(commitMessage, @"^.*?(([rR]elease|[hH]otfix|[aA]lpha)-|-v|/|/v|'|Finish )(?<PossibleVersions>(?<!://)\d+\.\d+(\.*\d+)*)")
                .Cast<Match>()
                .Select(m => m.Groups["PossibleVersions"].Value);

            return possibleVersions
                .Select(part =>
                {
                    SemanticVersion v;
                    return SemanticVersion.TryParse(part, configuration.GitTagPrefix, out v) ? v : null;
                }).FirstOrDefault(v => v != null);
        }
    }
}