using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace GitVersion.Core;

internal static partial class RegexPatterns
{
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.Compiled;

    public static class Cache
    {
        private static readonly ConcurrentDictionary<string, Regex> cache = new();

        public static Regex GetOrAdd([StringSyntax(StringSyntaxAttribute.Regex)] string pattern)
        {
            ArgumentNullException.ThrowIfNull(pattern);

            return cache.GetOrAdd(pattern, key =>
                KnownRegexes.TryGetValue(key, out var factory)
                    ? factory()
                    : new Regex(key, Options));
        }

        private static readonly ImmutableDictionary<string, Func<Regex>> KnownRegexes =
            new Dictionary<string, Func<Regex>>
            {
                [Common.SwitchArgumentRegexPattern] = Common.SwitchArgumentRegex,
                [Common.ObscurePasswordRegexPattern] = Common.ObscurePasswordRegex,
                [Common.ExpandTokensRegexPattern] = Common.ExpandTokensRegex,
                [Common.SanitizeEnvVarNameRegexPattern] = Common.SanitizeEnvVarNameRegex,
                [Common.SanitizeMemberNameRegexPattern] = Common.SanitizeMemberNameRegex,
                [Common.SanitizeNameRegexPattern] = Common.SanitizeNameRegex,
                [Configuration.DefaultTagPrefixRegexPattern] = Configuration.DefaultTagPrefixRegex,
                [Configuration.DefaultVersionInBranchRegexPattern] = Configuration.DefaultVersionInBranchRegex,
                [Configuration.MainBranchRegexPattern] = Configuration.MainBranchRegex,
                [Configuration.DevelopBranchRegexPattern] = Configuration.DevelopBranchRegex,
                [Configuration.ReleaseBranchRegexPattern] = Configuration.ReleaseBranchRegex,
                [Configuration.FeatureBranchRegexPattern] = Configuration.FeatureBranchRegex,
                [Configuration.PullRequestBranchRegexPattern] = Configuration.PullRequestBranchRegex,
                [Configuration.HotfixBranchRegexPattern] = Configuration.HotfixBranchRegex,
                [Configuration.SupportBranchRegexPattern] = Configuration.SupportBranchRegex,
                [Configuration.UnknownBranchRegexPattern] = Configuration.UnknownBranchRegex,
                [MergeMessage.DefaultMergeMessageRegexPattern] = MergeMessage.DefaultMergeMessageRegex,
                [MergeMessage.SmartGitMergeMessageRegexPattern] = MergeMessage.SmartGitMergeMessageRegex,
                [MergeMessage.BitBucketPullMergeMessageRegexPattern] = MergeMessage.BitBucketPullMergeMessageRegex,
                [MergeMessage.BitBucketPullv7MergeMessageRegexPattern] = MergeMessage.BitBucketPullv7MergeMessageRegex,
                [MergeMessage.BitBucketCloudPullMergeMessageRegexPattern] = MergeMessage.BitBucketCloudPullMergeMessageRegex,
                [MergeMessage.GitHubPullMergeMessageRegexPattern] = MergeMessage.GitHubPullMergeMessageRegex,
                [MergeMessage.RemoteTrackingMergeMessageRegexPattern] = MergeMessage.RemoteTrackingMergeMessageRegex,
                [MergeMessage.AzureDevOpsPullMergeMessageRegexPattern] = MergeMessage.AzureDevOpsPullMergeMessageRegex,
                [Output.AssemblyVersionRegexPattern] = Output.AssemblyVersionRegex,
                [Output.AssemblyInfoVersionRegexPattern] = Output.AssemblyInfoVersionRegex,
                [Output.AssemblyFileVersionRegexPattern] = Output.AssemblyFileVersionRegex,
                [Output.SanitizeAssemblyInfoRegexPattern] = Output.SanitizeAssemblyInfoRegex,
                [Output.CsharpAssemblyAttributeRegexPattern] = Output.CsharpAssemblyAttributeRegex,
                [Output.FsharpAssemblyAttributeRegexPattern] = Output.FsharpAssemblyAttributeRegex,
                [Output.VisualBasicAssemblyAttributeRegexPattern] = Output.VisualBasicAssemblyAttributeRegex,
                [Output.SanitizeParticipantRegexPattern] = Output.SanitizeParticipantRegex,
                [VersionCalculation.DefaultMajorRegexPattern] = VersionCalculation.DefaultMajorRegex,
                [VersionCalculation.DefaultMinorRegexPattern] = VersionCalculation.DefaultMinorRegex,
                [VersionCalculation.DefaultPatchRegexPattern] = VersionCalculation.DefaultPatchRegex,
                [VersionCalculation.DefaultNoBumpRegexPattern] = VersionCalculation.DefaultNoBumpRegex,
                [SemanticVersion.ParseStrictRegexPattern] = SemanticVersion.ParseStrictRegex,
                [SemanticVersion.ParseLooseRegexPattern] = SemanticVersion.ParseLooseRegex,
                [SemanticVersion.ParseBuildMetaDataRegexPattern] = SemanticVersion.ParseBuildMetaDataRegex,
                [SemanticVersion.FormatBuildMetaDataRegexPattern] = SemanticVersion.FormatBuildMetaDataRegex,
                [SemanticVersion.ParsePreReleaseTagRegexPattern] = SemanticVersion.ParsePreReleaseTagRegex,
                [AssemblyVersion.CSharp.TriviaRegexPattern] = AssemblyVersion.CSharp.TriviaRegex,
                [AssemblyVersion.CSharp.AttributeRegexPattern] = AssemblyVersion.CSharp.AttributeRegex,
                [AssemblyVersion.FSharp.TriviaRegexPattern] = AssemblyVersion.FSharp.TriviaRegex,
                // AssemblyVersion.FSharp.TriviaRegexPattern is same as C# so can't be added to the cache so C# TriviaRegex is used for F# as well.
                [AssemblyVersion.FSharp.AttributeRegexPattern] = AssemblyVersion.FSharp.AttributeRegex,
                [AssemblyVersion.VisualBasic.TriviaRegexPattern] = AssemblyVersion.VisualBasic.TriviaRegex,
                [AssemblyVersion.VisualBasic.AttributeRegexPattern] = AssemblyVersion.VisualBasic.AttributeRegex
            }.ToImmutableDictionary();
    }

    internal static partial class Common
    {
        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string SwitchArgumentRegexPattern = @"/\w+:";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string ObscurePasswordRegexPattern = "(https?://)(.+)(:.+@)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string ExpandTokensRegexPattern = """
            \{                              # Opening brace
                (?:                         # Start of either env or member expression
                    env:(?!env:)(?<envvar>[A-Za-z_][A-Za-z0-9_]*)       # Only a single env: prefix, not followed by another env:
                    |                                                   # OR
                    (?<member>[A-Za-z_][A-Za-z0-9_]*)                   # member/property name
                    (?:                                                 # Optional format specifier
                        :(?<format>[A-Za-z0-9\.\-,]+)                   # Colon followed by format string (no spaces, ?, or }), format cannot contain colon
                    )?                                                  # Format is optional
                )                                                       # End group for env or member
                (?:                                                     # Optional fallback group
                    \s*\?\?\s+                                          # '??' operator with optional whitespace: exactly two question marks for fallback
                    (?:                                                 # Fallback value alternatives:
                        (?<fallback>\w+)                                #   A single word fallback
                        |                                               # OR
                        "(?<fallback>[^"]*)"                            #   A quoted string fallback
                    )
                )?                                                      # Fallback is optional
            \}
            """;

        /// <summary>
        ///   Allow alphanumeric, underscore, colon (for custom format specification), hyphen, and dot
        /// </summary>
        [StringSyntax(StringSyntaxAttribute.Regex, Options)]
        internal const string SanitizeEnvVarNameRegexPattern = @"^[A-Za-z0-9_:\-\.]+$";

        /// <summary>
        ///   Allow alphanumeric, underscore, and dot for property/field access
        /// </summary>
        [StringSyntax(StringSyntaxAttribute.Regex, Options)]
        internal const string SanitizeMemberNameRegexPattern = @"^[A-Za-z0-9_\.]+$";

        [StringSyntax(StringSyntaxAttribute.Regex, Options)]
        internal const string SanitizeNameRegexPattern = "[^a-zA-Z0-9-]";

        [GeneratedRegex(SwitchArgumentRegexPattern, Options)]
        public static partial Regex SwitchArgumentRegex();

        [GeneratedRegex(ObscurePasswordRegexPattern, Options)]
        public static partial Regex ObscurePasswordRegex();

        [GeneratedRegex(ExpandTokensRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
        public static partial Regex ExpandTokensRegex();

        [GeneratedRegex(SanitizeEnvVarNameRegexPattern, Options)]
        public static partial Regex SanitizeEnvVarNameRegex();

        [GeneratedRegex(SanitizeMemberNameRegexPattern, Options)]
        public static partial Regex SanitizeMemberNameRegex();

        [GeneratedRegex(SanitizeNameRegexPattern, Options)]
        public static partial Regex SanitizeNameRegex();
    }

    internal static partial class Configuration
    {
        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string DefaultTagPrefixRegexPattern = "[vV]?";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string DefaultVersionInBranchRegexPattern = @"(?<version>[vV]?\d+(\.\d+)?(\.\d+)?).*";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string MainBranchRegexPattern = "^master$|^main$";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string DevelopBranchRegexPattern = "^dev(elop)?(ment)?$";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string ReleaseBranchRegexPattern = @"^releases?[\/-](?<BranchName>.+)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string FeatureBranchRegexPattern = @"^features?[\/-](?<BranchName>.+)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string PullRequestBranchRegexPattern = @"^(pull-requests|pull|pr)[\/-](?<Number>\d*)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string HotfixBranchRegexPattern = @"^hotfix(es)?[\/-](?<BranchName>.+)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string SupportBranchRegexPattern = @"^support[\/-](?<BranchName>.+)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string UnknownBranchRegexPattern = "(?<BranchName>.+)";

        [GeneratedRegex(DefaultTagPrefixRegexPattern, Options)]
        public static partial Regex DefaultTagPrefixRegex();

        [GeneratedRegex(DefaultVersionInBranchRegexPattern, Options)]
        public static partial Regex DefaultVersionInBranchRegex();

        [GeneratedRegex(MainBranchRegexPattern, Options)]
        public static partial Regex MainBranchRegex();

        [GeneratedRegex(DevelopBranchRegexPattern, Options)]
        public static partial Regex DevelopBranchRegex();

        [GeneratedRegex(ReleaseBranchRegexPattern, Options)]
        public static partial Regex ReleaseBranchRegex();

        [GeneratedRegex(FeatureBranchRegexPattern, Options)]
        public static partial Regex FeatureBranchRegex();

        [GeneratedRegex(PullRequestBranchRegexPattern, Options)]
        public static partial Regex PullRequestBranchRegex();

        [GeneratedRegex(HotfixBranchRegexPattern, Options)]
        public static partial Regex HotfixBranchRegex();

        [GeneratedRegex(SupportBranchRegexPattern, Options)]
        public static partial Regex SupportBranchRegex();

        [GeneratedRegex(UnknownBranchRegexPattern, Options)]
        public static partial Regex UnknownBranchRegex();
    }

    internal static partial class MergeMessage
    {
        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string DefaultMergeMessageRegexPattern = @"^Merge (branch|tag) '(?<SourceBranch>[^']*)'(?: into (?<TargetBranch>[^\s]*))*";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string SmartGitMergeMessageRegexPattern = @"^Finish (?<SourceBranch>[^\s]*)(?: into (?<TargetBranch>[^\s]*))*";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string BitBucketPullMergeMessageRegexPattern = @"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?<Source>.*) from (?<SourceBranch>[^\s]*) to (?<TargetBranch>[^\s]*)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string BitBucketPullv7MergeMessageRegexPattern = @"^Pull request #(?<PullRequestNumber>\d+).*\r?\n\r?\nMerge in (?<Source>.*) from (?<SourceBranch>[^\s]*) to (?<TargetBranch>[^\s]*)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string BitBucketCloudPullMergeMessageRegexPattern = @"^Merged in (?<SourceBranch>[^\s]*) \(pull request #(?<PullRequestNumber>\d+)\)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string GitHubPullMergeMessageRegexPattern = @"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?:[^\s\/]+\/)?(?<SourceBranch>[^\s]*)(?: into (?<TargetBranch>[^\s]*))*";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string RemoteTrackingMergeMessageRegexPattern = @"^Merge remote-tracking branch '(?<SourceBranch>[^\s]*)'(?: into (?<TargetBranch>[^\s]*))*";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string AzureDevOpsPullMergeMessageRegexPattern = @"^Merge pull request (?<PullRequestNumber>\d+) from (?<SourceBranch>[^\s]*) into (?<TargetBranch>[^\s]*)";

        [GeneratedRegex(DefaultMergeMessageRegexPattern, Options)]
        public static partial Regex DefaultMergeMessageRegex();

        [GeneratedRegex(SmartGitMergeMessageRegexPattern, Options)]
        public static partial Regex SmartGitMergeMessageRegex();

        [GeneratedRegex(BitBucketPullMergeMessageRegexPattern, Options)]
        public static partial Regex BitBucketPullMergeMessageRegex();

        [GeneratedRegex(BitBucketPullv7MergeMessageRegexPattern, Options)]
        public static partial Regex BitBucketPullv7MergeMessageRegex();

        [GeneratedRegex(BitBucketCloudPullMergeMessageRegexPattern, Options)]
        public static partial Regex BitBucketCloudPullMergeMessageRegex();

        [GeneratedRegex(GitHubPullMergeMessageRegexPattern, Options)]
        public static partial Regex GitHubPullMergeMessageRegex();

        [GeneratedRegex(RemoteTrackingMergeMessageRegexPattern, Options)]
        public static partial Regex RemoteTrackingMergeMessageRegex();

        [GeneratedRegex(AzureDevOpsPullMergeMessageRegexPattern, Options)]
        public static partial Regex AzureDevOpsPullMergeMessageRegex();
    }

    internal static partial class Output
    {
        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string AssemblyVersionRegexPattern = @"AssemblyVersion(Attribute)?\s*\(.*\)\s*";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string AssemblyInfoVersionRegexPattern = @"AssemblyInformationalVersion(Attribute)?\s*\(.*\)\s*";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string AssemblyFileVersionRegexPattern = @"AssemblyFileVersion(Attribute)?\s*\(.*\)\s*";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string CsharpAssemblyAttributeRegexPattern = @"(\s*\[\s*assembly:\s*(?:.*)\s*\]\s*$(\r?\n)?)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string FsharpAssemblyAttributeRegexPattern = @"(\s*\[\s*\<assembly:\s*(?:.*)\>\s*\]\s*$(\r?\n)?)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string VisualBasicAssemblyAttributeRegexPattern = @"(\s*\<Assembly:\s*(?:.*)\>\s*$(\r?\n)?)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string SanitizeParticipantRegexPattern = "[^a-zA-Z0-9]";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string SanitizeAssemblyInfoRegexPattern = "[^0-9A-Za-z-.+]";

        [GeneratedRegex(AssemblyVersionRegexPattern, Options)]
        public static partial Regex AssemblyVersionRegex();

        [GeneratedRegex(AssemblyInfoVersionRegexPattern, Options)]
        public static partial Regex AssemblyInfoVersionRegex();

        [GeneratedRegex(AssemblyFileVersionRegexPattern, Options)]
        public static partial Regex AssemblyFileVersionRegex();

        [GeneratedRegex(CsharpAssemblyAttributeRegexPattern, Options | RegexOptions.Multiline)]
        public static partial Regex CsharpAssemblyAttributeRegex();

        [GeneratedRegex(FsharpAssemblyAttributeRegexPattern, Options | RegexOptions.Multiline)]
        public static partial Regex FsharpAssemblyAttributeRegex();

        [GeneratedRegex(VisualBasicAssemblyAttributeRegexPattern, Options | RegexOptions.Multiline)]
        public static partial Regex VisualBasicAssemblyAttributeRegex();

        [GeneratedRegex(SanitizeParticipantRegexPattern, Options)]
        public static partial Regex SanitizeParticipantRegex();

        [GeneratedRegex(SanitizeAssemblyInfoRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
        public static partial Regex SanitizeAssemblyInfoRegex();
    }

    internal static partial class VersionCalculation
    {
        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string DefaultMajorRegexPattern = @"\+semver:\s?(breaking|major)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string DefaultMinorRegexPattern = @"\+semver:\s?(feature|minor)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string DefaultPatchRegexPattern = @"\+semver:\s?(fix|patch)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string DefaultNoBumpRegexPattern = @"\+semver:\s?(none|skip)";

        [GeneratedRegex(DefaultMajorRegexPattern, Options)]
        public static partial Regex DefaultMajorRegex();

        [GeneratedRegex(DefaultMinorRegexPattern, Options)]
        public static partial Regex DefaultMinorRegex();

        [GeneratedRegex(DefaultPatchRegexPattern, Options)]
        public static partial Regex DefaultPatchRegex();

        [GeneratedRegex(DefaultNoBumpRegexPattern, Options)]
        public static partial Regex DefaultNoBumpRegex();
    }

    internal static partial class SemanticVersion
    {
        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string ParseStrictRegexPattern = @"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string ParseLooseRegexPattern = @"^(?<SemVer>(?<Major>\d+)(\.(?<Minor>\d+))?(\.(?<Patch>\d+))?)(\.(?<FourthPart>\d+))?(-(?<Tag>[^\+]*))?(\+(?<BuildMetaData>.*))?$";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string ParseBuildMetaDataRegexPattern = @"(?<BuildNumber>\d+)?(\.?Branch(Name)?\.(?<BranchName>[^\.]+))?(\.?Sha?\.(?<Sha>[^\.]+))?(?<Other>.*)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string FormatBuildMetaDataRegexPattern = "[^0-9A-Za-z-.]";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        internal const string ParsePreReleaseTagRegexPattern = @"(?<name>.*?)\.?(?<number>\d+)?$";

        // uses the git-semver spec https://github.com/semver/semver/blob/master/semver.md
        [GeneratedRegex(ParseStrictRegexPattern, Options)]
        public static partial Regex ParseStrictRegex();

        [GeneratedRegex(ParseLooseRegexPattern, Options)]
        public static partial Regex ParseLooseRegex();

        [GeneratedRegex(ParseBuildMetaDataRegexPattern, Options)]
        public static partial Regex ParseBuildMetaDataRegex();

        [GeneratedRegex(FormatBuildMetaDataRegexPattern, Options)]
        public static partial Regex FormatBuildMetaDataRegex();

        [GeneratedRegex(ParsePreReleaseTagRegexPattern, Options)]
        public static partial Regex ParsePreReleaseTagRegex();
    }

    internal static partial class AssemblyVersion
    {
        internal static partial class CSharp
        {
            [StringSyntax(StringSyntaxAttribute.Regex)]
            internal const string TriviaRegexPattern =
                """
                /\*(.*?)\*/                    # Block comments: matches /* ... */
                |//(.*?)\r?\n                  # Line comments: matches // ... followed by a newline
                |"((\\[^\n]|[^"\n])*)"         # Strings: matches " ... " including escaped quotes
                """;

            [StringSyntax(StringSyntaxAttribute.Regex)]
            internal const string AttributeRegexPattern =
                """
                (?x)                                  # IgnorePatternWhitespace
                \[\s*assembly\s*:\s*                  # The [assembly: part
                (System\s*\.\s*Reflection\s*\.\s*)?   # The System.Reflection. part (optional)
                Assembly(File|Informational)?Version  # The attribute AssemblyVersion, AssemblyFileVersion, or AssemblyInformationalVersion
                \s*\(\s*\)\s*\]                       # End brackets ()]
                """;

            [GeneratedRegex(TriviaRegexPattern, RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | Options)]
            public static partial Regex TriviaRegex();

            [GeneratedRegex(AttributeRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
            public static partial Regex AttributeRegex();
        }

        internal static partial class FSharp
        {
            [StringSyntax(StringSyntaxAttribute.Regex)]
            internal const string TriviaRegexPattern =
                """
                /\*(.*?)\*/                     # Block comments: matches /* ... */
                |//(.*?)\r?\n                   # Line comments: matches // ... followed by a newline
                |"((\\[^\n]|[^"\n])*)"          # Strings: matches " ... " including escaped quotes
                """;

            [StringSyntax(StringSyntaxAttribute.Regex)]
            internal const string AttributeRegexPattern =
                """
                (?x)                                    # IgnorePatternWhitespace
                \[\s*<\s*assembly\s*:\s*                # The [<assembly: part
                (System\s*\.\s*Reflection\s*\.\s*)?     # The System.Reflection. part (optional)
                Assembly(File|Informational)?Version    # The attribute AssemblyVersion, AssemblyFileVersion, or AssemblyInformationalVersion
                \s*\(\s*\)\s*>\s*\]                     # End brackets ()>]
                """;

            /// <summary>
            /// Note that while available to call direct, as the C# TriviaRegex is the same it will handle any calls through the cache for F# too.
            /// </summary>
            /// <returns></returns>
            [GeneratedRegex(TriviaRegexPattern, RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | Options)]
            public static partial Regex TriviaRegex();

            [GeneratedRegex(AttributeRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
            public static partial Regex AttributeRegex();
        }

        internal static partial class VisualBasic
        {
            [StringSyntax(StringSyntaxAttribute.Regex)]
            internal const string TriviaRegexPattern =
                """
                '(.*?)\r?\n                     # Line comments: matches // ... followed by a newline
                |"((\\[^\n]|[^"\n])*)"          # Strings: matches " ... " including escaped quotes
                """;

            [StringSyntax(StringSyntaxAttribute.Regex)]
            internal const string AttributeRegexPattern =
                """
                (?x) # IgnorePatternWhitespace
                \<\s*Assembly\s*:\s*                  # The <Assembly: part
                (System\s*\.\s*Reflection\s*\.\s*)?   # The System.Reflection. part (optional)
                Assembly(File|Informational)?Version  # The attribute AssemblyVersion, AssemblyFileVersion, or AssemblyInformationalVersion
                \s*\(\s*\)\s*\>                       # End brackets ()>
                """;

            [GeneratedRegex(TriviaRegexPattern, RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | Options)]
            public static partial Regex TriviaRegex();

            [GeneratedRegex(AttributeRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
            public static partial Regex AttributeRegex();
        }
    }
}
