using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace GitVersion;

internal static partial class RegexPatterns
{
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.Compiled;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2); // unified timeout for non-GeneratedRegex fallbacks

    [StringSyntax(StringSyntaxAttribute.Regex)]
    private const string SwitchArgumentRegexPattern = @"/\w+:";

    [StringSyntax(StringSyntaxAttribute.Regex)]
    private const string ObscurePasswordRegexPattern = "(https?://)(.+)(:.+@)";

    [StringSyntax(StringSyntaxAttribute.Regex)]
    private const string ExpandTokensRegexPattern = @"\{([^{}]+)\}";

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

    [StringSyntax(StringSyntaxAttribute.Regex, Options)]
    internal const string SanitizeLabelRegexPattern = "[^a-zA-Z0-9-.]";

    [GeneratedRegex(SwitchArgumentRegexPattern, Options)]
    public static partial Regex SwitchArgumentRegex { get; }

    [GeneratedRegex(ObscurePasswordRegexPattern, Options)]
    public static partial Regex ObscurePasswordRegex { get; }

    [GeneratedRegex(ExpandTokensRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
    public static partial Regex ExpandTokensRegex { get; }

    [GeneratedRegex(SanitizeEnvVarNameRegexPattern, Options)]
    public static partial Regex SanitizeEnvVarNameRegex { get; }

    [GeneratedRegex(SanitizeMemberNameRegexPattern, Options)]
    public static partial Regex SanitizeMemberNameRegex { get; }

    [GeneratedRegex(SanitizeNameRegexPattern, Options)]
    public static partial Regex SanitizeNameRegex { get; }

    public static class Cache
    {
        private static readonly ConcurrentDictionary<string, Regex> cache = new();

        public static Regex GetOrAdd([StringSyntax(StringSyntaxAttribute.Regex)] string pattern)
        {
            ArgumentNullException.ThrowIfNull(pattern);

            return cache.GetOrAdd(pattern, key =>
                KnownRegexes.TryGetValue(key, out var regex)
                    ? regex
                    : new Regex(key, Options, DefaultTimeout)); // now uses timeout for safety
        }

        // Descriptor used to centralize pattern + compiled regex instance. Extendable with options/timeout metadata later.
        private readonly record struct RegexDescriptor(string Pattern, Regex Regex);

        // Central descriptor list – single source of truth for known patterns. Order not significant.
        private static readonly RegexDescriptor[] Descriptors =
        [
            new(SwitchArgumentRegexPattern, SwitchArgumentRegex),
            new(ObscurePasswordRegexPattern, ObscurePasswordRegex),
            new(ExpandTokensRegexPattern, ExpandTokensRegex),
            new(SanitizeEnvVarNameRegexPattern, SanitizeEnvVarNameRegex),
            new(SanitizeMemberNameRegexPattern, SanitizeMemberNameRegex),
            new(SanitizeNameRegexPattern, SanitizeNameRegex),
            new(Configuration.DefaultTagPrefixRegexPattern, Configuration.DefaultTagPrefixRegex),
            new(Configuration.DefaultVersionInBranchRegexPattern, Configuration.DefaultVersionInBranchRegex),
            new(Configuration.MainBranchRegexPattern, Configuration.MainBranchRegex),
            new(Configuration.DevelopBranchRegexPattern, Configuration.DevelopBranchRegex),
            new(Configuration.ReleaseBranchRegexPattern, Configuration.ReleaseBranchRegex),
            new(Configuration.FeatureBranchRegexPattern, Configuration.FeatureBranchRegex),
            new(Configuration.PullRequestBranchRegexPattern, Configuration.PullRequestBranchRegex),
            new(Configuration.HotfixBranchRegexPattern, Configuration.HotfixBranchRegex),
            new(Configuration.SupportBranchRegexPattern, Configuration.SupportBranchRegex),
            new(Configuration.UnknownBranchRegexPattern, Configuration.UnknownBranchRegex),
            new(MergeMessage.DefaultMergeMessageRegexPattern, MergeMessage.DefaultMergeMessageRegex),
            new(MergeMessage.SmartGitMergeMessageRegexPattern, MergeMessage.SmartGitMergeMessageRegex),
            new(MergeMessage.BitBucketPullMergeMessageRegexPattern, MergeMessage.BitBucketPullMergeMessageRegex),
            new(MergeMessage.BitBucketPullv7MergeMessageRegexPattern, MergeMessage.BitBucketPullv7MergeMessageRegex),
            new(MergeMessage.BitBucketCloudPullMergeMessageRegexPattern, MergeMessage.BitBucketCloudPullMergeMessageRegex),
            new(MergeMessage.GitHubPullMergeMessageRegexPattern, MergeMessage.GitHubPullMergeMessageRegex),
            new(MergeMessage.RemoteTrackingMergeMessageRegexPattern, MergeMessage.RemoteTrackingMergeMessageRegex),
            new(MergeMessage.AzureDevOpsPullMergeMessageRegexPattern, MergeMessage.AzureDevOpsPullMergeMessageRegex),
            new(Output.AssemblyVersionRegexPattern, Output.AssemblyVersionRegex),
            new(Output.AssemblyInfoVersionRegexPattern, Output.AssemblyInfoVersionRegex),
            new(Output.AssemblyFileVersionRegexPattern, Output.AssemblyFileVersionRegex),
            new(Output.SanitizeAssemblyInfoRegexPattern, Output.SanitizeAssemblyInfoRegex),
            new(Output.CsharpAssemblyAttributeRegexPattern, Output.CsharpAssemblyAttributeRegex),
            new(Output.FsharpAssemblyAttributeRegexPattern, Output.FsharpAssemblyAttributeRegex),
            new(Output.VisualBasicAssemblyAttributeRegexPattern, Output.VisualBasicAssemblyAttributeRegex),
            new(Output.SanitizeParticipantRegexPattern, Output.SanitizeParticipantRegex),
            new(VersionCalculation.DefaultMajorRegexPattern, VersionCalculation.DefaultMajorRegex),
            new(VersionCalculation.DefaultMinorRegexPattern, VersionCalculation.DefaultMinorRegex),
            new(VersionCalculation.DefaultPatchRegexPattern, VersionCalculation.DefaultPatchRegex),
            new(VersionCalculation.DefaultNoBumpRegexPattern, VersionCalculation.DefaultNoBumpRegex),
            new(SemanticVersion.ParseStrictRegexPattern, SemanticVersion.ParseStrictRegex),
            new(SemanticVersion.ParseLooseRegexPattern, SemanticVersion.ParseLooseRegex),
            new(SemanticVersion.ParseBuildMetaDataRegexPattern, SemanticVersion.ParseBuildMetaDataRegex),
            new(SemanticVersion.FormatBuildMetaDataRegexPattern, SemanticVersion.FormatBuildMetaDataRegex),
            new(SemanticVersion.ParsePreReleaseTagRegexPattern, SemanticVersion.ParsePreReleaseTagRegex),
            // Trivia pattern unified: C# & F# share same underlying pattern; only map once under C# constant.
            new(AssemblyVersion.CSharp.TriviaRegexPattern, AssemblyVersion.CSharp.TriviaRegex),
            new(AssemblyVersion.CSharp.AttributeRegexPattern, AssemblyVersion.CSharp.AttributeRegex),
            // F# Trivia pattern identical – Attribute differs, so include attribute pattern only.
            new(AssemblyVersion.FSharp.AttributeRegexPattern, AssemblyVersion.FSharp.AttributeRegex),
            new(AssemblyVersion.VisualBasic.TriviaRegexPattern, AssemblyVersion.VisualBasic.TriviaRegex),
            new(AssemblyVersion.VisualBasic.AttributeRegexPattern, AssemblyVersion.VisualBasic.AttributeRegex)
        ];

        private static readonly ImmutableDictionary<string, Regex> KnownRegexes =
            Descriptors.ToImmutableDictionary(d => d.Pattern, d => d.Regex);
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
        public static partial Regex DefaultTagPrefixRegex { get; }

        [GeneratedRegex(DefaultVersionInBranchRegexPattern, Options)]
        public static partial Regex DefaultVersionInBranchRegex { get; }

        [GeneratedRegex(MainBranchRegexPattern, Options)]
        public static partial Regex MainBranchRegex { get; }

        [GeneratedRegex(DevelopBranchRegexPattern, Options)]
        public static partial Regex DevelopBranchRegex { get; }

        [GeneratedRegex(ReleaseBranchRegexPattern, Options)]
        public static partial Regex ReleaseBranchRegex { get; }

        [GeneratedRegex(FeatureBranchRegexPattern, Options)]
        public static partial Regex FeatureBranchRegex { get; }

        [GeneratedRegex(PullRequestBranchRegexPattern, Options)]
        public static partial Regex PullRequestBranchRegex { get; }

        [GeneratedRegex(HotfixBranchRegexPattern, Options)]
        public static partial Regex HotfixBranchRegex { get; }

        [GeneratedRegex(SupportBranchRegexPattern, Options)]
        public static partial Regex SupportBranchRegex { get; }

        [GeneratedRegex(UnknownBranchRegexPattern, Options)]
        public static partial Regex UnknownBranchRegex { get; }
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
        public static partial Regex DefaultMergeMessageRegex { get; }

        [GeneratedRegex(SmartGitMergeMessageRegexPattern, Options)]
        public static partial Regex SmartGitMergeMessageRegex { get; }

        [GeneratedRegex(BitBucketPullMergeMessageRegexPattern, Options)]
        public static partial Regex BitBucketPullMergeMessageRegex { get; }

        [GeneratedRegex(BitBucketPullv7MergeMessageRegexPattern, Options)]
        public static partial Regex BitBucketPullv7MergeMessageRegex { get; }

        [GeneratedRegex(BitBucketCloudPullMergeMessageRegexPattern, Options)]
        public static partial Regex BitBucketCloudPullMergeMessageRegex { get; }

        [GeneratedRegex(GitHubPullMergeMessageRegexPattern, Options)]
        public static partial Regex GitHubPullMergeMessageRegex { get; }

        [GeneratedRegex(RemoteTrackingMergeMessageRegexPattern, Options)]
        public static partial Regex RemoteTrackingMergeMessageRegex { get; }

        [GeneratedRegex(AzureDevOpsPullMergeMessageRegexPattern, Options)]
        public static partial Regex AzureDevOpsPullMergeMessageRegex { get; }
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
        public static partial Regex AssemblyVersionRegex { get; }

        [GeneratedRegex(AssemblyInfoVersionRegexPattern, Options)]
        public static partial Regex AssemblyInfoVersionRegex { get; }

        [GeneratedRegex(AssemblyFileVersionRegexPattern, Options)]
        public static partial Regex AssemblyFileVersionRegex { get; }

        [GeneratedRegex(CsharpAssemblyAttributeRegexPattern, Options | RegexOptions.Multiline)]
        public static partial Regex CsharpAssemblyAttributeRegex { get; }

        [GeneratedRegex(FsharpAssemblyAttributeRegexPattern, Options | RegexOptions.Multiline)]
        public static partial Regex FsharpAssemblyAttributeRegex { get; }

        [GeneratedRegex(VisualBasicAssemblyAttributeRegexPattern, Options | RegexOptions.Multiline)]
        public static partial Regex VisualBasicAssemblyAttributeRegex { get; }

        [GeneratedRegex(SanitizeParticipantRegexPattern, Options)]
        public static partial Regex SanitizeParticipantRegex { get; }

        [GeneratedRegex(SanitizeAssemblyInfoRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
        public static partial Regex SanitizeAssemblyInfoRegex { get; }
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
        public static partial Regex DefaultMajorRegex { get; }

        [GeneratedRegex(DefaultMinorRegexPattern, Options)]
        public static partial Regex DefaultMinorRegex { get; }

        [GeneratedRegex(DefaultPatchRegexPattern, Options)]
        public static partial Regex DefaultPatchRegex { get; }

        [GeneratedRegex(DefaultNoBumpRegexPattern, Options)]
        public static partial Regex DefaultNoBumpRegex { get; }
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
        public static partial Regex ParseStrictRegex { get; }

        [GeneratedRegex(ParseLooseRegexPattern, Options)]
        public static partial Regex ParseLooseRegex { get; }

        [GeneratedRegex(ParseBuildMetaDataRegexPattern, Options)]
        public static partial Regex ParseBuildMetaDataRegex { get; }

        [GeneratedRegex(FormatBuildMetaDataRegexPattern, Options)]
        public static partial Regex FormatBuildMetaDataRegex { get; }

        [GeneratedRegex(ParsePreReleaseTagRegexPattern, Options)]
        public static partial Regex ParsePreReleaseTagRegex { get; }
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
            public static partial Regex TriviaRegex { get; }

            [GeneratedRegex(AttributeRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
            public static partial Regex AttributeRegex { get; }
        }

        internal static partial class FSharp
        {
            [StringSyntax(StringSyntaxAttribute.Regex)]
            internal const string TriviaRegexPattern = CSharp.TriviaRegexPattern; // unified

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
            public static partial Regex TriviaRegex { get; }

            [GeneratedRegex(AttributeRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
            public static partial Regex AttributeRegex { get; }
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
            public static partial Regex TriviaRegex { get; }

            [GeneratedRegex(AttributeRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
            public static partial Regex AttributeRegex { get; }
        }
    }
}
