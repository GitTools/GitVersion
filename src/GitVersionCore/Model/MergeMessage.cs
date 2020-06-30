using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GitVersion.Model.Configuration;

namespace GitVersion
{
    public class MergeMessage
    {
        private static readonly IList<MergeMessageFormat> DefaultFormats = new List<MergeMessageFormat>
        {
            new MergeMessageFormat("Default", @"^Merge (branch|tag) '(?<SourceBranch>[^']*)'(?: into (?<TargetBranch>[^\s]*))*"),
            new MergeMessageFormat("SmartGit",  @"^Finish (?<SourceBranch>[^\s]*)(?: into (?<TargetBranch>[^\s]*))*"),
            new MergeMessageFormat("BitBucketPull", @"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?<Source>.*) from (?<SourceBranch>[^\s]*) to (?<TargetBranch>[^\s]*)"),
            new MergeMessageFormat("BitBucketPullv7", @"^Pull request #(?<PullRequestNumber>\d+).*\r?\n\r?\nMerge in (?<Source>.*) from (?<SourceBranch>[^\s]*) to (?<TargetBranch>[^\s]*)"),
            new MergeMessageFormat("GitHubPull", @"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?:(?<SourceBranch>[^\s]*))(?: into (?<TargetBranch>[^\s]*))*"),
            new MergeMessageFormat("RemoteTracking", @"^Merge remote-tracking branch '(?<SourceBranch>[^\s]*)'(?: into (?<TargetBranch>[^\s]*))*")
        };

        public MergeMessage(string mergeMessage, Config config)
        {
            if (mergeMessage == null)
                throw new NullReferenceException();

            // Concat config formats with the defaults.
            // Ensure configs are processed first.
            var allFormats = config.MergeMessageFormats
                .Select(x => new MergeMessageFormat(x.Key, x.Value))
                .Concat(DefaultFormats);

            foreach (var format in allFormats)
            {
                var match = format.Pattern.Match(mergeMessage);
                if (match.Success)
                {
                    FormatName = format.Name;
                    MergedBranch = match.Groups["SourceBranch"].Value;

                    if (match.Groups["TargetBranch"].Success)
                    {
                        TargetBranch = match.Groups["TargetBranch"].Value;
                    }

                    if (int.TryParse(match.Groups["PullRequestNumber"].Value, out var pullNumber))
                    {
                        PullRequestNumber = pullNumber;
                    }

                    Version = ParseVersion(config.TagPrefix);

                    break;
                }
            }
        }

        public string FormatName { get; }
        public string TargetBranch { get; }
        public string MergedBranch { get; } = "";
        public bool IsMergedPullRequest => PullRequestNumber != null;
        public int? PullRequestNumber { get; }
        public SemanticVersion Version { get; }

        private SemanticVersion ParseVersion(string tagPrefix)
        {
            // Remove remotes and branch prefixes like release/ feature/ hotfix/ etc
            var toMatch = Regex.Replace(MergedBranch, @"^(\w+[-/])*", "", RegexOptions.IgnoreCase);
            toMatch = Regex.Replace(toMatch, $"^{tagPrefix}", "");
            // We don't match if the version is likely an ip (i.e starts with http://)
            var versionMatch = new Regex(@"^(?<!://)\d+\.\d+(\.*\d+)*");
            var version = versionMatch.Match(toMatch);

            if (version.Success && SemanticVersion.TryParse(version.Value, tagPrefix, out var val))
            {
                return val;
            }

            return null;
        }

        private class MergeMessageFormat
        {
            public MergeMessageFormat(string name, string pattern)
            {
                Name = name;
                Pattern = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }

            public string Name { get; }

            public Regex Pattern { get; }
        }
    }
}
