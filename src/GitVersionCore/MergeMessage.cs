using System;
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
        static Regex parseBitBucketPullMergeMessage = new Regex(
            @"^Merge pull request #(?<PullRequestNumber>\d*) (from|in) (?<Source>.*) from (?<SourceBranch>.*) to (?<TargetBranch>.*)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex smartGitMergeMessage = new Regex(
            @"^Finish (?<Branch>.*)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex parseRemoteTrackingMergeMessage = new Regex(
            @"^Merge remote-tracking branch '(?<SourceBranch>.*)'( into (?<TargetBranch>.*))?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static Regex parseTfsMergeMessageEnglishUS = new Regex(
            @"^Merge (?<SourceBranch>.*) to (?<TargetBranch>.*)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        // Zusammengeführter PR \"9\": release/5.0.1 mit master mergen
        static Regex parseTfsMergeMessageGermanDE = new Regex(
            @"^Zusammengeführter PR ""(?<PullRequestNumber>\d*)""\: (?<SourceBranch>.*) mit (?<TargetBranch>.*) mergen",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private string mergeMessage;

        public MergeMessage(string mergeMessage, Config config)
        {
            this.mergeMessage = mergeMessage;

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

            match = parseBitBucketPullMergeMessage.Match(mergeMessage);
            if (match.Success)
            {
                IsMergedPullRequest = true;
                PullRequestNumber = GetPullRequestNumber(match);
                return match.Groups["SourceBranch"].Value;
            }

            match = parseGitHubPullMergeMessage.Match(mergeMessage);
            if (match.Success)
            {
                IsMergedPullRequest = true;
                PullRequestNumber = GetPullRequestNumber(match);
                var from = match.Groups["Source"].Value;
                // TODO We could remove/separate the remote name at this point?
                return from;
            }

            match = parseRemoteTrackingMergeMessage.Match(mergeMessage);
            if (match.Success)
            {
                var from = match.Groups["SourceBranch"].Value;
                // TODO We could remove/separate the remote name at this point?
                return from;
            }

            match = parseTfsMergeMessageEnglishUS.Match(mergeMessage);
            if (match.Success)
            {
                IsMergedPullRequest = true;
                var from = match.Groups["SourceBranch"].Value;
                return from;
            }

            match = parseTfsMergeMessageGermanDE.Match(mergeMessage);
            if (match.Success)
            {
                IsMergedPullRequest = true;
                var from = match.Groups["SourceBranch"].Value;
                return from;
            }

            return "";
        }

        private int GetPullRequestNumber(Match match)
        {
            int pullNumber;
            if (int.TryParse(match.Groups["PullRequestNumber"].Value, out pullNumber))
            {
                PullRequestNumber = pullNumber;
            }
            return pullNumber;
        }

        public string TargetBranch { get; }
        public string MergedBranch { get; }
        public bool IsMergedPullRequest { get; private set; }
        public int? PullRequestNumber { get; private set; }
        public SemanticVersion Version { get; }
    }
}
