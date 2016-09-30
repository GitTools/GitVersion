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
            var commitsPriorToThan = context.CurrentBranch
                .CommitsPriorToThan(context.CurrentCommit.When());
            var baseVersions = commitsPriorToThan
                .SelectMany(c =>
                {
                    SemanticVersion semanticVersion;
                    if (TryParse(c, context.Configuration, out semanticVersion))
                    {
                        var shouldIncrement = !context.Configuration.PreventIncrementForMergedBranchVersion;
                        return new[]
                        {
                            new BaseVersion(context, string.Format("Merge message '{0}'", c.Message.Trim()), shouldIncrement, semanticVersion, c, null)
                        };
                    }
                    return Enumerable.Empty<BaseVersion>();
                }).ToList();
            return baseVersions;
        }

        static bool TryParse(Commit mergeCommit, EffectiveConfiguration configuration, out SemanticVersion semanticVersion)
        {
            semanticVersion = Inner(mergeCommit, configuration);
            return semanticVersion != null;
        }

        static SemanticVersion Inner(Commit mergeCommit, EffectiveConfiguration configuration)
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