namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System.Linq;
    using System.Text.RegularExpressions;
    using LibGit2Sharp;

    public class MergeMessageBaseVersionStrategy : BaseVersionStrategy
    {
        public override BaseVersion GetVersion(GitVersionContext context)
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
                            new BaseVersion(string.Format("Merge message '{0}'", c.Message.Trim()), shouldIncrement, semanticVersion, c, null)
                        };
                    }
                    return Enumerable.Empty<BaseVersion>();
                })
                .ToArray();

            return baseVersions.Length > 1 ? baseVersions.Aggregate((x, y) => x.SemanticVersion > y.SemanticVersion ? x : y) : baseVersions.SingleOrDefault();
        }

        static bool TryParse(Commit mergeCommit, EffectiveConfiguration configuration, out SemanticVersion semanticVersion)
        {
            semanticVersion = Inner(mergeCommit, configuration);
            return semanticVersion != null;
        }

        private static SemanticVersion Inner(Commit mergeCommit, EffectiveConfiguration configuration)
        {
            if (mergeCommit.Parents.Count() < 2)
            {
                return null;
            }

            var possibleVersions = Regex.Matches(mergeCommit.Message, @"^.*?(-|/|'|Finish )(?<PossibleVersions>\d+\.\d+\.\d+)")
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