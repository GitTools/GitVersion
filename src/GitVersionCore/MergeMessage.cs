using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace GitVersion
{
    class MergeMessage
    {
        static Regex parseMergeMessage = new Regex(
            @"^Merge (branch|tag) '(?<Branch>[^']*)'",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex parseGitHubPullMergeMessage = new Regex(
            @"^Merge pull request #(?<PullRequestNumber>\d*) (from|in) (?<Source>.*)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex smartGitMergeMessage = new Regex(
            @"^Finish (?<Branch>.*)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private string mergeMessage;
        private Config config;

        public MergeMessage(string mergeMessage, Config config)
        {
            this.mergeMessage = mergeMessage;
            this.config = config;

            var lastIndexOf = mergeMessage.LastIndexOf("into", StringComparison.OrdinalIgnoreCase);
            if (lastIndexOf != -1)
            {
                // If we have into in the merge message the rest should be the target branch
                TargetBranch = mergeMessage.Substring(lastIndexOf + 5);
            }

            MergedBranch = ParseBranch();

            // Remove remotes and branch prefixes like release/ feature/ hotfix/ etc
            var toMatch = Regex.Replace(MergedBranch, @"^(\w+[-/])*", "", RegexOptions.IgnoreCase);
            toMatch = Regex.Replace(toMatch, $"^{config.TagPrefix}", "");
            // We don't match if the version is likely an ip (i.e starts with http://)
            var versionMatch = new Regex(@"^(?<!://)\d+\.\d+(\.*\d+)*");
            var version = versionMatch.Match(toMatch);

            if (version.Success)
            {
                SemanticVersion val;
                if (SemanticVersion.TryParse(version.Value, config.TagPrefix, out val))
                {
                    Version = val;
                }
            }
        }

        private string ParseBranch()
        {
            var match = parseMergeMessage.Match(mergeMessage);
            if (match.Success)
            {
                return match.Groups["Branch"].Value;
            }

            match = smartGitMergeMessage.Match(mergeMessage);
            if (match.Success)
            {
                return match.Groups["Branch"].Value;
            }

            match = parseGitHubPullMergeMessage.Match(mergeMessage);
            if (match.Success)
            {
                IsMergedPullRequest = true;
                int pullNumber;
                if (int.TryParse(match.Groups["PullRequestNumber"].Value, out pullNumber))
                {
                    PullRequestNumber = pullNumber;
                }
                var from = match.Groups["Source"].Value;
                // We could remove/separate the remote name at this point?
                return from;
            }

            return "";
        }

        public string TargetBranch { get; }
        public string MergedBranch { get; }
        public bool IsMergedPullRequest { get; private set; }
        public int? PullRequestNumber { get; private set; }
        public SemanticVersion Version { get; }
    }
}
