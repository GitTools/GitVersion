using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace GitVersion.Core;

[SuppressMessage("Performance", "SYSLIB1045:Convert to \'GeneratedRegexAttribute\'.")]
internal static class RegexPatterns
{
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.Compiled;

    internal static readonly ConcurrentDictionary<string, Regex> Cache = new();

    static RegexPatterns()
    {
        Cache.TryAdd(Common.SwitchArgumentRegex.ToString(), Common.SwitchArgumentRegex);
        Cache.TryAdd(Common.ObscurePasswordRegex.ToString(), Common.ObscurePasswordRegex);
        Cache.TryAdd(Common.ExpandTokensRegex.ToString(), Common.ExpandTokensRegex);

        Cache.TryAdd(Configuration.DefaultTagPrefixRegex.ToString(), Configuration.DefaultTagPrefixRegex);
        Cache.TryAdd(Configuration.DefaultVersionInBranchRegex.ToString(), Configuration.DefaultVersionInBranchRegex);
        Cache.TryAdd(Configuration.MainBranchRegex.ToString(), Configuration.MainBranchRegex);
        Cache.TryAdd(Configuration.DevelopBranchRegex.ToString(), Configuration.DevelopBranchRegex);
        Cache.TryAdd(Configuration.ReleaseBranchRegex.ToString(), Configuration.ReleaseBranchRegex);
        Cache.TryAdd(Configuration.FeatureBranchRegex.ToString(), Configuration.FeatureBranchRegex);
        Cache.TryAdd(Configuration.PullRequestBranchRegex.ToString(), Configuration.PullRequestBranchRegex);
        Cache.TryAdd(Configuration.HotfixBranchRegex.ToString(), Configuration.HotfixBranchRegex);
        Cache.TryAdd(Configuration.SupportBranchRegex.ToString(), Configuration.SupportBranchRegex);
        Cache.TryAdd(Configuration.UnknownBranchRegex.ToString(), Configuration.UnknownBranchRegex);

        Cache.TryAdd(MergeMessage.DefaultMergeMessageRegex.ToString(), MergeMessage.DefaultMergeMessageRegex);
        Cache.TryAdd(MergeMessage.SmartGitMergeMessageRegex.ToString(), MergeMessage.SmartGitMergeMessageRegex);
        Cache.TryAdd(MergeMessage.BitBucketPullMergeMessageRegex.ToString(), MergeMessage.BitBucketPullMergeMessageRegex);
        Cache.TryAdd(MergeMessage.BitBucketPullv7MergeMessageRegex.ToString(), MergeMessage.BitBucketPullv7MergeMessageRegex);
        Cache.TryAdd(MergeMessage.BitBucketCloudPullMergeMessageRegex.ToString(), MergeMessage.BitBucketCloudPullMergeMessageRegex);
        Cache.TryAdd(MergeMessage.GitHubPullMergeMessageRegex.ToString(), MergeMessage.GitHubPullMergeMessageRegex);
        Cache.TryAdd(MergeMessage.RemoteTrackingMergeMessageRegex.ToString(), MergeMessage.RemoteTrackingMergeMessageRegex);
        Cache.TryAdd(MergeMessage.AzureDevOpsPullMergeMessageRegex.ToString(), MergeMessage.AzureDevOpsPullMergeMessageRegex);

        Cache.TryAdd(Output.AssemblyVersionRegex.ToString(), Output.AssemblyVersionRegex);
        Cache.TryAdd(Output.AssemblyInfoVersionRegex.ToString(), Output.AssemblyInfoVersionRegex);
        Cache.TryAdd(Output.AssemblyFileVersionRegex.ToString(), Output.AssemblyFileVersionRegex);
        Cache.TryAdd(Output.CsharpAssemblyAttributeRegex.ToString(), Output.CsharpAssemblyAttributeRegex);
        Cache.TryAdd(Output.FsharpAssemblyAttributeRegex.ToString(), Output.FsharpAssemblyAttributeRegex);
        Cache.TryAdd(Output.VisualBasicAssemblyAttributeRegex.ToString(), Output.VisualBasicAssemblyAttributeRegex);

        Cache.TryAdd(VersionCalculation.DefaultMajorRegex.ToString(), VersionCalculation.DefaultMajorRegex);
        Cache.TryAdd(VersionCalculation.DefaultMinorRegex.ToString(), VersionCalculation.DefaultMinorRegex);
        Cache.TryAdd(VersionCalculation.DefaultPatchRegex.ToString(), VersionCalculation.DefaultPatchRegex);
        Cache.TryAdd(VersionCalculation.DefaultNoBumpRegex.ToString(), VersionCalculation.DefaultNoBumpRegex);

        Cache.TryAdd(SemanticVersion.ParseStrictRegex.ToString(), SemanticVersion.ParseStrictRegex);
        Cache.TryAdd(SemanticVersion.ParseLooseRegex.ToString(), SemanticVersion.ParseLooseRegex);
        Cache.TryAdd(SemanticVersion.ParseBuildMetaDataRegex.ToString(), SemanticVersion.ParseBuildMetaDataRegex);
        Cache.TryAdd(SemanticVersion.FormatBuildMetaDataRegex.ToString(), SemanticVersion.FormatBuildMetaDataRegex);
        Cache.TryAdd(SemanticVersion.ParsePreReleaseTagRegex.ToString(), SemanticVersion.ParsePreReleaseTagRegex);

        Cache.TryAdd(AssemblyVersion.CSharp.TriviaRegex.ToString(), AssemblyVersion.CSharp.TriviaRegex);
        Cache.TryAdd(AssemblyVersion.CSharp.AttributeRegex.ToString(), AssemblyVersion.CSharp.AttributeRegex);
        Cache.TryAdd(AssemblyVersion.FSharp.TriviaRegex.ToString(), AssemblyVersion.FSharp.TriviaRegex);
        Cache.TryAdd(AssemblyVersion.FSharp.AttributeRegex.ToString(), AssemblyVersion.FSharp.AttributeRegex);
        Cache.TryAdd(AssemblyVersion.VisualBasic.TriviaRegex.ToString(), AssemblyVersion.VisualBasic.TriviaRegex);
        Cache.TryAdd(AssemblyVersion.VisualBasic.AttributeRegex.ToString(), AssemblyVersion.VisualBasic.AttributeRegex);
    }

    internal static class Common
    {
        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string SwitchArgumentRegexPattern = @"/\w+:";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string ObscurePasswordRegexPattern = "(https?://)(.+)(:.+@)";

        // this regex matches an expression to replace.
        // - env:ENV name OR a member name
        // - optional fallback value after " ?? "
        // - the fallback value should be a quoted string, but simple unquoted text is allowed for back compat
        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string ExpandTokensRegexPattern = """{((env:(?<envvar>\w+))|(?<member>\w+))(\s+(\?\?)??\s+((?<fallback>\w+)|"(?<fallback>.*)"))??}""";

        public static Regex SwitchArgumentRegex { get; } = new(SwitchArgumentRegexPattern, Options);
        public static Regex ObscurePasswordRegex { get; } = new(ObscurePasswordRegexPattern, Options);
        public static Regex ExpandTokensRegex { get; } = new(ExpandTokensRegexPattern, Options);
    }

    internal static class Configuration
    {
        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string DefaultTagPrefixPattern = "[vV]?";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string DefaultVersionInBranchPattern = @"(?<version>[vV]?\d+(\.\d+)?(\.\d+)?).*";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string MainBranchRegexPattern = "^master$|^main$";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string DevelopBranchRegexPattern = "^dev(elop)?(ment)?$";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string ReleaseBranchRegexPattern = @"^releases?[\/-](?<BranchName>.+)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string FeatureBranchRegexPattern = @"^features?[\/-](?<BranchName>.+)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string PullRequestBranchRegexPattern = @"^(pull-requests|pull|pr)[\/-](?<Number>\d*)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string HotfixBranchRegexPattern = @"^hotfix(es)?[\/-](?<BranchName>.+)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string SupportBranchRegexPattern = @"^support[\/-](?<BranchName>.+)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string UnknownBranchRegexPattern = "(?<BranchName>.+)";

        public static Regex DefaultTagPrefixRegex { get; } = new(DefaultTagPrefixPattern, Options);
        public static Regex DefaultVersionInBranchRegex { get; } = new(DefaultVersionInBranchPattern, Options);
        public static Regex MainBranchRegex { get; } = new(MainBranchRegexPattern, Options);
        public static Regex DevelopBranchRegex { get; } = new(DevelopBranchRegexPattern, Options);
        public static Regex ReleaseBranchRegex { get; } = new(ReleaseBranchRegexPattern, Options);
        public static Regex FeatureBranchRegex { get; } = new(FeatureBranchRegexPattern, Options);
        public static Regex PullRequestBranchRegex { get; } = new(PullRequestBranchRegexPattern, Options);
        public static Regex HotfixBranchRegex { get; } = new(HotfixBranchRegexPattern, Options);
        public static Regex SupportBranchRegex { get; } = new(SupportBranchRegexPattern, Options);
        public static Regex UnknownBranchRegex { get; } = new(UnknownBranchRegexPattern, Options);
    }

    internal static class MergeMessage
    {
        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string DefaultMergeMessageRegexPattern = @"^Merge (branch|tag) '(?<SourceBranch>[^']*)'(?: into (?<TargetBranch>[^\s]*))*";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string SmartGitMergeMessageRegexPattern = @"^Finish (?<SourceBranch>[^\s]*)(?: into (?<TargetBranch>[^\s]*))*";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string BitBucketPullMergeMessageRegexPattern = @"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?<Source>.*) from (?<SourceBranch>[^\s]*) to (?<TargetBranch>[^\s]*)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string BitBucketPullv7MergeMessageRegexPattern = @"^Pull request #(?<PullRequestNumber>\d+).*\r?\n\r?\nMerge in (?<Source>.*) from (?<SourceBranch>[^\s]*) to (?<TargetBranch>[^\s]*)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string BitBucketCloudPullMergeMessageRegexPattern = @"^Merged in (?<SourceBranch>[^\s]*) \(pull request #(?<PullRequestNumber>\d+)\)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string GitHubPullMergeMessageRegexPattern = @"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?:[^\s\/]+\/)?(?<SourceBranch>[^\s]*)(?: into (?<TargetBranch>[^\s]*))*";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string RemoteTrackingMergeMessageRegexPattern = @"^Merge remote-tracking branch '(?<SourceBranch>[^\s]*)'(?: into (?<TargetBranch>[^\s]*))*";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string AzureDevOpsPullMergeMessageRegexPattern = @"^Merge pull request (?<PullRequestNumber>\d+) from (?<SourceBranch>[^\s]*) into (?<TargetBranch>[^\s]*)";

        public static Regex DefaultMergeMessageRegex { get; } = new(DefaultMergeMessageRegexPattern, Options);
        public static Regex SmartGitMergeMessageRegex { get; } = new(SmartGitMergeMessageRegexPattern, Options);
        public static Regex BitBucketPullMergeMessageRegex { get; } = new(BitBucketPullMergeMessageRegexPattern, Options);
        public static Regex BitBucketPullv7MergeMessageRegex { get; } = new(BitBucketPullv7MergeMessageRegexPattern, Options);
        public static Regex BitBucketCloudPullMergeMessageRegex { get; } = new(BitBucketCloudPullMergeMessageRegexPattern, Options);
        public static Regex GitHubPullMergeMessageRegex { get; } = new(GitHubPullMergeMessageRegexPattern, Options);
        public static Regex RemoteTrackingMergeMessageRegex { get; } = new(RemoteTrackingMergeMessageRegexPattern, Options);
        public static Regex AzureDevOpsPullMergeMessageRegex { get; } = new(AzureDevOpsPullMergeMessageRegexPattern, Options);
    }

    internal static class Output
    {
        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string AssemblyVersionRegexPattern = @"AssemblyVersion(Attribute)?\s*\(.*\)\s*";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string AssemblyInfoVersionRegexPattern = @"AssemblyInformationalVersion(Attribute)?\s*\(.*\)\s*";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string AssemblyFileVersionRegexPattern = @"AssemblyFileVersion(Attribute)?\s*\(.*\)\s*";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string CsharpAssemblyAttributeRegexPattern = @"(\s*\[\s*assembly:\s*(?:.*)\s*\]\s*$(\r?\n)?)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string FsharpAssemblyAttributeRegexPattern = @"(\s*\[\s*\<assembly:\s*(?:.*)\>\s*\]\s*$(\r?\n)?)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string VisualBasicAssemblyAttributeRegexPattern = @"(\s*\<Assembly:\s*(?:.*)\>\s*$(\r?\n)?)";

        public static Regex AssemblyVersionRegex { get; } = new(AssemblyVersionRegexPattern, Options);
        public static Regex AssemblyInfoVersionRegex { get; } = new(AssemblyInfoVersionRegexPattern, Options);
        public static Regex AssemblyFileVersionRegex { get; } = new(AssemblyFileVersionRegexPattern, Options);
        public static Regex CsharpAssemblyAttributeRegex { get; } = new(CsharpAssemblyAttributeRegexPattern, Options | RegexOptions.Multiline);
        public static Regex FsharpAssemblyAttributeRegex { get; } = new(FsharpAssemblyAttributeRegexPattern, Options | RegexOptions.Multiline);
        public static Regex VisualBasicAssemblyAttributeRegex { get; } = new(VisualBasicAssemblyAttributeRegexPattern, Options | RegexOptions.Multiline);
    }

    internal static class VersionCalculation
    {
        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string DefaultMajorPattern = @"\+semver:\s?(breaking|major)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string DefaultMinorPattern = @"\+semver:\s?(feature|minor)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string DefaultPatchPattern = @"\+semver:\s?(fix|patch)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string DefaultNoBumpPattern = @"\+semver:\s?(none|skip)";

        public static Regex DefaultMajorRegex { get; } = new(DefaultMajorPattern, Options);
        public static Regex DefaultMinorRegex { get; } = new(DefaultMinorPattern, Options);
        public static Regex DefaultPatchRegex { get; } = new(DefaultPatchPattern, Options);
        public static Regex DefaultNoBumpRegex { get; } = new(DefaultNoBumpPattern, Options);
    }

    internal static class SemanticVersion
    {
        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string ParseStrictRegexPattern = @"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string ParseLooseRegexPattern = @"^(?<SemVer>(?<Major>\d+)(\.(?<Minor>\d+))?(\.(?<Patch>\d+))?)(\.(?<FourthPart>\d+))?(-(?<Tag>[^\+]*))?(\+(?<BuildMetaData>.*))?$";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string ParseBuildMetaDataRegexPattern = @"(?<BuildNumber>\d+)?(\.?Branch(Name)?\.(?<BranchName>[^\.]+))?(\.?Sha?\.(?<Sha>[^\.]+))?(?<Other>.*)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string FormatBuildMetaDataRegexPattern = "[^0-9A-Za-z-.]";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        private const string ParsePreReleaseTagRegexPattern = @"(?<name>.*?)\.?(?<number>\d+)?$";

        // uses the git-semver spec https://github.com/semver/semver/blob/master/semver.md
        public static Regex ParseStrictRegex { get; } = new(ParseStrictRegexPattern, Options);

        public static Regex ParseLooseRegex { get; } = new(ParseLooseRegexPattern, Options);

        public static Regex ParseBuildMetaDataRegex { get; } = new(ParseBuildMetaDataRegexPattern, Options);

        public static Regex FormatBuildMetaDataRegex { get; } = new(FormatBuildMetaDataRegexPattern, Options);

        public static Regex ParsePreReleaseTagRegex { get; } = new(ParsePreReleaseTagRegexPattern, Options);
    }

    internal static class AssemblyVersion
    {
        internal static class CSharp
        {
            [StringSyntax(StringSyntaxAttribute.Regex)]
            private const string TriviaRegexPattern =
                """
                /\*(.*?)\*/                    # Block comments: matches /* ... */
                |//(.*?)\r?\n                  # Line comments: matches // ... followed by a newline
                |"((\\[^\n]|[^"\n])*)"         # Strings: matches " ... " including escaped quotes
                """;

            [StringSyntax(StringSyntaxAttribute.Regex)]
            private const string AttributeRegexPattern =
                """
                (?x)                                  # IgnorePatternWhitespace
                \[\s*assembly\s*:\s*                  # The [assembly: part
                (System\s*\.\s*Reflection\s*\.\s*)?   # The System.Reflection. part (optional)
                Assembly(File|Informational)?Version  # The attribute AssemblyVersion, AssemblyFileVersion, or AssemblyInformationalVersion
                \s*\(\s*\)\s*\]                       # End brackets ()]
                """;

            public static Regex TriviaRegex { get; } = new(TriviaRegexPattern, RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | Options);
            public static Regex AttributeRegex { get; } = new(AttributeRegexPattern, RegexOptions.IgnorePatternWhitespace | Options);
        }

        internal static class FSharp
        {
            [StringSyntax(StringSyntaxAttribute.Regex)]
            private const string TriviaRegexPattern =
                """
                /\*(.*?)\*/                     # Block comments: matches /* ... */
                |//(.*?)\r?\n                   # Line comments: matches // ... followed by a newline
                |"((\\[^\n]|[^"\n])*)"          # Strings: matches " ... " including escaped quotes
                """;

            [StringSyntax(StringSyntaxAttribute.Regex)]
            private const string AttributeRegexPattern =
                """
                (?x)                                    # IgnorePatternWhitespace
                \[\s*<\s*assembly\s*:\s*                # The [<assembly: part
                (System\s*\.\s*Reflection\s*\.\s*)?     # The System.Reflection. part (optional)
                Assembly(File|Informational)?Version    # The attribute AssemblyVersion, AssemblyFileVersion, or AssemblyInformationalVersion
                \s*\(\s*\)\s*>\s*\]                     # End brackets ()>]
                """;

            public static Regex TriviaRegex { get; } = new(TriviaRegexPattern, RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | Options);
            public static Regex AttributeRegex { get; } = new(AttributeRegexPattern, RegexOptions.IgnorePatternWhitespace | Options);
        }

        internal static class VisualBasic
        {
            [StringSyntax(StringSyntaxAttribute.Regex)]
            private const string TriviaRegexPattern =
                """
                '(.*?)\r?\n                     # Line comments: matches // ... followed by a newline
                |"((\\[^\n]|[^"\n])*)"          # Strings: matches " ... " including escaped quotes
                """;

            [StringSyntax(StringSyntaxAttribute.Regex)]
            private const string AttributeRegexPattern =
                """
                (?x) # IgnorePatternWhitespace
                \<\s*Assembly\s*:\s*                  # The <Assembly: part
                (System\s*\.\s*Reflection\s*\.\s*)?   # The System.Reflection. part (optional)
                Assembly(File|Informational)?Version  # The attribute AssemblyVersion, AssemblyFileVersion, or AssemblyInformationalVersion
                \s*\(\s*\)\s*\>                       # End brackets ()>
                """;

            public static Regex TriviaRegex { get; } = new(TriviaRegexPattern, RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | Options);
            public static Regex AttributeRegex { get; } = new(AttributeRegexPattern, RegexOptions.IgnorePatternWhitespace | Options);
        }
    }
}
