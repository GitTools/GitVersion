namespace GitVersion.VersionCalculation.BaseVersionCalculators
{
    using System;
    using System.Linq;
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
            string versionPart;
            if (Inner(mergeCommit, out versionPart))
            {
                return SemanticVersion.TryParse(versionPart, configuration.GitTagPrefix, out semanticVersion);
            }
            semanticVersion = null;
            return false;
        }

        static bool Inner(Commit mergeCommit, out string versionPart)
        {
            if (mergeCommit.Parents.Count() < 2)
            {
                versionPart = null;
                return false;
            }

            var version = mergeCommit
                .Message.Split('/', '-', '\'', '"', ' ')
                .Select(part =>
                {
                    SemanticVersion v;
                    return SemanticVersion.TryParse(part, "", out v) ? v : null;
                }).FirstOrDefault(v => v != null)
                ;

            versionPart = version!=null ? version.ToString() : null;

            return versionPart != null;
        }

        static bool TryGetPrefix(string target, out string result, string splitter)
        {
            var indexOf = target.IndexOf(splitter, StringComparison.Ordinal);
            if (indexOf == -1)
            {
                result = null;
                return false;
            }
            result = target.Substring(0, indexOf);
            return true;
        }

        static bool TryGetSuffix(string target, out string result, string splitter)
        {
            var indexOf = target.IndexOf(splitter, StringComparison.Ordinal);
            if (indexOf == -1)
            {
                result = null;
                return false;
            }
            result = target.Substring(indexOf + 1, target.Length - indexOf - 1);
            return true;
        }
    }
}