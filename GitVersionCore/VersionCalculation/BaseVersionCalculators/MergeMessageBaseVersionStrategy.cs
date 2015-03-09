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

            var message = mergeCommit.Message.TrimToFirstLine();

            var knownMergePrefixes = new[] { "Merge branch 'hotfix-", "Merge branch 'hotfix/", "Merge branch 'release-", "Merge branch 'release/" };

            foreach (var prefix in knownMergePrefixes)
            {
                if (message.StartsWith(prefix))
            if (message.StartsWith("Merge tag '"))
            {
                var suffix = message.Replace("Merge tag '", "");

                if (suffix.Contains("-"))
                {
                    suffix = suffix.Split('-')[1];
                }
                return TryGetPrefix(suffix, out versionPart, "'");
            }

                {
                    var suffix = message.Substring(prefix.Length);
                    return TryGetPrefix(suffix, out versionPart, "'");
                }
            }
            
            if (message.StartsWith("Merge branch '"))
            {
                var suffix = message.Replace("Merge branch '", "");

                if (suffix.Contains("-"))
                {
                    suffix = suffix.Split('-')[1];
                }
                return TryGetPrefix(suffix, out versionPart, "'");
            }

            if (message.StartsWith("Merge pull request #"))
            {
                var split = message.Split(new[]
                {
                    "/"
                }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length != 2)
                {
                    versionPart = null;
                    return false;
                }
                return TryGetSuffix(split[1], out versionPart, "-");
            }

            if (message.StartsWith("Finish Release-")) //Match Syntevo SmartGit client's GitFlow 'release' merge commit message formatting
            {
                versionPart = message.Replace("Finish Release-", "");
                return true;
            }

            if (message.StartsWith("Finish ")) //Match Syntevo SmartGit client's GitFlow 'hotfix' merge commit message formatting
            {
                versionPart = message.Replace("Finish ", "");
                return true;
            }

            versionPart = null;
            return false;
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