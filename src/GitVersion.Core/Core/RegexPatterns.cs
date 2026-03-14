using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace GitVersion.Core;

internal static partial class RegexPatterns
{
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.Compiled;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2); // unified timeout for non-GeneratedRegex fallbacks

    [StringSyntax(StringSyntaxAttribute.Regex)]
    private const string SwitchArgumentRegexPattern = @"/\w+:";

    [StringSyntax(StringSyntaxAttribute.Regex)]
    private const string ObscurePasswordRegexPattern = "(https?://)(.+)(:.+@)";

    [StringSyntax(StringSyntaxAttribute.Regex)]
    private const string ExpandTokensRegexPattern =
        """
        \{                              # Opening brace
            (?:                         # Start of either env or member expression
                env:(?!env:)(?<envvar>[A-Za-z_][A-Za-z0-9_]*)       # Only a single env: prefix, not followed by another env:
                |                                                   # OR
                (?<member>[A-Za-z_][A-Za-z0-9_]*)                   # member/property name
                (?:                                                 # Optional format specifier
                    :(?<format>[A-Za-z0-9\.\-,;'"]+)                # Colon followed by format string (including semicolons and quotes for legacy composite format)
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

#if NET9_0_OR_GREATER
    [GeneratedRegex(SwitchArgumentRegexPattern, Options)]
    public static partial Regex SwitchArgumentRegex { get; }
#else
    [GeneratedRegex(SwitchArgumentRegexPattern, Options)]
    private static partial Regex SwitchArgumentRegexImpl();

    public static Regex SwitchArgumentRegex { get; } = SwitchArgumentRegexImpl();
#endif

#if NET9_0_OR_GREATER
    [GeneratedRegex(ObscurePasswordRegexPattern, Options)]
    public static partial Regex ObscurePasswordRegex { get; }
#else
    [GeneratedRegex(ObscurePasswordRegexPattern, Options)]
    private static partial Regex ObscurePasswordRegexImpl();

    public static Regex ObscurePasswordRegex { get; } = ObscurePasswordRegexImpl();
#endif

#if NET9_0_OR_GREATER
    [GeneratedRegex(ExpandTokensRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
    public static partial Regex ExpandTokensRegex { get; }
#else
    [GeneratedRegex(ExpandTokensRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
    private static partial Regex ExpandTokensRegexImpl();

    public static Regex ExpandTokensRegex { get; } = ExpandTokensRegexImpl();
#endif

#if NET9_0_OR_GREATER
    [GeneratedRegex(SanitizeEnvVarNameRegexPattern, Options)]
    public static partial Regex SanitizeEnvVarNameRegex { get; }
#else
    [GeneratedRegex(SanitizeEnvVarNameRegexPattern, Options)]
    private static partial Regex SanitizeEnvVarNameRegexImpl();

    public static Regex SanitizeEnvVarNameRegex { get; } = SanitizeEnvVarNameRegexImpl();
#endif

#if NET9_0_OR_GREATER
    [GeneratedRegex(SanitizeMemberNameRegexPattern, Options)]
    public static partial Regex SanitizeMemberNameRegex { get; }
#else
    [GeneratedRegex(SanitizeMemberNameRegexPattern, Options)]
    private static partial Regex SanitizeMemberNameRegexImpl();

    public static Regex SanitizeMemberNameRegex { get; } = SanitizeMemberNameRegexImpl();
#endif

#if NET9_0_OR_GREATER
    [GeneratedRegex(SanitizeNameRegexPattern, Options)]
    public static partial Regex SanitizeNameRegex { get; }
#else
    [GeneratedRegex(SanitizeNameRegexPattern, Options)]
    private static partial Regex SanitizeNameRegexImpl();

    public static Regex SanitizeNameRegex { get; } = SanitizeNameRegexImpl();
#endif

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

#if NET9_0_OR_GREATER
        [GeneratedRegex(DefaultTagPrefixRegexPattern, Options)]
        public static partial Regex DefaultTagPrefixRegex { get; }
#else
        [GeneratedRegex(DefaultTagPrefixRegexPattern, Options)]
        private static partial Regex DefaultTagPrefixRegexImpl();

        public static Regex DefaultTagPrefixRegex { get; } = DefaultTagPrefixRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(DefaultVersionInBranchRegexPattern, Options)]
        public static partial Regex DefaultVersionInBranchRegex { get; }
#else
        [GeneratedRegex(DefaultVersionInBranchRegexPattern, Options)]
        private static partial Regex DefaultVersionInBranchRegexImpl();

        public static Regex DefaultVersionInBranchRegex { get; } = DefaultVersionInBranchRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(MainBranchRegexPattern, Options)]
        public static partial Regex MainBranchRegex { get; }
#else
        [GeneratedRegex(MainBranchRegexPattern, Options)]
        private static partial Regex MainBranchRegexImpl();

        public static Regex MainBranchRegex { get; } = MainBranchRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(DevelopBranchRegexPattern, Options)]
        public static partial Regex DevelopBranchRegex { get; }
#else
        [GeneratedRegex(DevelopBranchRegexPattern, Options)]
        private static partial Regex DevelopBranchRegexImpl();

        public static Regex DevelopBranchRegex { get; } = DevelopBranchRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(ReleaseBranchRegexPattern, Options)]
        public static partial Regex ReleaseBranchRegex { get; }
#else
        [GeneratedRegex(ReleaseBranchRegexPattern, Options)]
        private static partial Regex ReleaseBranchRegexImpl();

        public static Regex ReleaseBranchRegex { get; } = ReleaseBranchRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(FeatureBranchRegexPattern, Options)]
        public static partial Regex FeatureBranchRegex { get; }
#else
        [GeneratedRegex(FeatureBranchRegexPattern, Options)]
        private static partial Regex FeatureBranchRegexImpl();

        public static Regex FeatureBranchRegex { get; } = FeatureBranchRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(PullRequestBranchRegexPattern, Options)]
        public static partial Regex PullRequestBranchRegex { get; }
#else
        [GeneratedRegex(PullRequestBranchRegexPattern, Options)]
        private static partial Regex PullRequestBranchRegexImpl();

        public static Regex PullRequestBranchRegex { get; } = PullRequestBranchRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(HotfixBranchRegexPattern, Options)]
        public static partial Regex HotfixBranchRegex { get; }
#else
        [GeneratedRegex(HotfixBranchRegexPattern, Options)]
        private static partial Regex HotfixBranchRegexImpl();

        public static Regex HotfixBranchRegex { get; } = HotfixBranchRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(SupportBranchRegexPattern, Options)]
        public static partial Regex SupportBranchRegex { get; }
#else
        [GeneratedRegex(SupportBranchRegexPattern, Options)]
        private static partial Regex SupportBranchRegexImpl();

        public static Regex SupportBranchRegex { get; } = SupportBranchRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(UnknownBranchRegexPattern, Options)]
        public static partial Regex UnknownBranchRegex { get; }
#else
        [GeneratedRegex(UnknownBranchRegexPattern, Options)]
        private static partial Regex UnknownBranchRegexImpl();

        public static Regex UnknownBranchRegex { get; } = UnknownBranchRegexImpl();
#endif
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

#if NET9_0_OR_GREATER
        [GeneratedRegex(DefaultMergeMessageRegexPattern, Options)]
        public static partial Regex DefaultMergeMessageRegex { get; }
#else
        [GeneratedRegex(DefaultMergeMessageRegexPattern, Options)]
        private static partial Regex DefaultMergeMessageRegexImpl();

        public static Regex DefaultMergeMessageRegex { get; } = DefaultMergeMessageRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(SmartGitMergeMessageRegexPattern, Options)]
        public static partial Regex SmartGitMergeMessageRegex { get; }
#else
        [GeneratedRegex(SmartGitMergeMessageRegexPattern, Options)]
        private static partial Regex SmartGitMergeMessageRegexImpl();

        public static Regex SmartGitMergeMessageRegex { get; } = SmartGitMergeMessageRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(BitBucketPullMergeMessageRegexPattern, Options)]
        public static partial Regex BitBucketPullMergeMessageRegex { get; }
#else
        [GeneratedRegex(BitBucketPullMergeMessageRegexPattern, Options)]
        private static partial Regex BitBucketPullMergeMessageRegexImpl();

        public static Regex BitBucketPullMergeMessageRegex { get; } = BitBucketPullMergeMessageRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(BitBucketPullv7MergeMessageRegexPattern, Options)]
        public static partial Regex BitBucketPullv7MergeMessageRegex { get; }
#else
        [GeneratedRegex(BitBucketPullv7MergeMessageRegexPattern, Options)]
        private static partial Regex BitBucketPullv7MergeMessageRegexImpl();

        public static Regex BitBucketPullv7MergeMessageRegex { get; } = BitBucketPullv7MergeMessageRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(BitBucketCloudPullMergeMessageRegexPattern, Options)]
        public static partial Regex BitBucketCloudPullMergeMessageRegex { get; }
#else
        [GeneratedRegex(BitBucketCloudPullMergeMessageRegexPattern, Options)]
        private static partial Regex BitBucketCloudPullMergeMessageRegexImpl();

        public static Regex BitBucketCloudPullMergeMessageRegex { get; } = BitBucketCloudPullMergeMessageRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(GitHubPullMergeMessageRegexPattern, Options)]
        public static partial Regex GitHubPullMergeMessageRegex { get; }
#else
        [GeneratedRegex(GitHubPullMergeMessageRegexPattern, Options)]
        private static partial Regex GitHubPullMergeMessageRegexImpl();

        public static Regex GitHubPullMergeMessageRegex { get; } = GitHubPullMergeMessageRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(RemoteTrackingMergeMessageRegexPattern, Options)]
        public static partial Regex RemoteTrackingMergeMessageRegex { get; }
#else
        [GeneratedRegex(RemoteTrackingMergeMessageRegexPattern, Options)]
        private static partial Regex RemoteTrackingMergeMessageRegexImpl();

        public static Regex RemoteTrackingMergeMessageRegex { get; } = RemoteTrackingMergeMessageRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(AzureDevOpsPullMergeMessageRegexPattern, Options)]
        public static partial Regex AzureDevOpsPullMergeMessageRegex { get; }
#else
        [GeneratedRegex(AzureDevOpsPullMergeMessageRegexPattern, Options)]
        private static partial Regex AzureDevOpsPullMergeMessageRegexImpl();

        public static Regex AzureDevOpsPullMergeMessageRegex { get; } = AzureDevOpsPullMergeMessageRegexImpl();
#endif
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

#if NET9_0_OR_GREATER
        [GeneratedRegex(AssemblyVersionRegexPattern, Options)]
        public static partial Regex AssemblyVersionRegex { get; }
#else
        [GeneratedRegex(AssemblyVersionRegexPattern, Options)]
        private static partial Regex AssemblyVersionRegexImpl();

        public static Regex AssemblyVersionRegex { get; } = AssemblyVersionRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(AssemblyInfoVersionRegexPattern, Options)]
        public static partial Regex AssemblyInfoVersionRegex { get; }
#else
        [GeneratedRegex(AssemblyInfoVersionRegexPattern, Options)]
        private static partial Regex AssemblyInfoVersionRegexImpl();

        public static Regex AssemblyInfoVersionRegex { get; } = AssemblyInfoVersionRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(AssemblyFileVersionRegexPattern, Options)]
        public static partial Regex AssemblyFileVersionRegex { get; }
#else
        [GeneratedRegex(AssemblyFileVersionRegexPattern, Options)]
        private static partial Regex AssemblyFileVersionRegexImpl();

        public static Regex AssemblyFileVersionRegex { get; } = AssemblyFileVersionRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(CsharpAssemblyAttributeRegexPattern, Options | RegexOptions.Multiline)]
        public static partial Regex CsharpAssemblyAttributeRegex { get; }
#else
        [GeneratedRegex(CsharpAssemblyAttributeRegexPattern, Options | RegexOptions.Multiline)]
        private static partial Regex CsharpAssemblyAttributeRegexImpl();

        public static Regex CsharpAssemblyAttributeRegex { get; } = CsharpAssemblyAttributeRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(FsharpAssemblyAttributeRegexPattern, Options | RegexOptions.Multiline)]
        public static partial Regex FsharpAssemblyAttributeRegex { get; }
#else
        [GeneratedRegex(FsharpAssemblyAttributeRegexPattern, Options | RegexOptions.Multiline)]
        private static partial Regex FsharpAssemblyAttributeRegexImpl();

        public static Regex FsharpAssemblyAttributeRegex { get; } = FsharpAssemblyAttributeRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(VisualBasicAssemblyAttributeRegexPattern, Options | RegexOptions.Multiline)]
        public static partial Regex VisualBasicAssemblyAttributeRegex { get; }
#else
        [GeneratedRegex(VisualBasicAssemblyAttributeRegexPattern, Options | RegexOptions.Multiline)]
        private static partial Regex VisualBasicAssemblyAttributeRegexImpl();

        public static Regex VisualBasicAssemblyAttributeRegex { get; } = VisualBasicAssemblyAttributeRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(SanitizeParticipantRegexPattern, Options)]
        public static partial Regex SanitizeParticipantRegex { get; }
#else
        [GeneratedRegex(SanitizeParticipantRegexPattern, Options)]
        private static partial Regex SanitizeParticipantRegexImpl();

        public static Regex SanitizeParticipantRegex { get; } = SanitizeParticipantRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(SanitizeAssemblyInfoRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
        public static partial Regex SanitizeAssemblyInfoRegex { get; }
#else
        [GeneratedRegex(SanitizeAssemblyInfoRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
        private static partial Regex SanitizeAssemblyInfoRegexImpl();

        public static Regex SanitizeAssemblyInfoRegex { get; } = SanitizeAssemblyInfoRegexImpl();
#endif
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

#if NET9_0_OR_GREATER
        [GeneratedRegex(DefaultMajorRegexPattern, Options)]
        public static partial Regex DefaultMajorRegex { get; }
#else
        [GeneratedRegex(DefaultMajorRegexPattern, Options)]
        private static partial Regex DefaultMajorRegexImpl();

        public static Regex DefaultMajorRegex { get; } = DefaultMajorRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(DefaultMinorRegexPattern, Options)]
        public static partial Regex DefaultMinorRegex { get; }
#else
        [GeneratedRegex(DefaultMinorRegexPattern, Options)]
        private static partial Regex DefaultMinorRegexImpl();

        public static Regex DefaultMinorRegex { get; } = DefaultMinorRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(DefaultPatchRegexPattern, Options)]
        public static partial Regex DefaultPatchRegex { get; }
#else
        [GeneratedRegex(DefaultPatchRegexPattern, Options)]
        private static partial Regex DefaultPatchRegexImpl();

        public static Regex DefaultPatchRegex { get; } = DefaultPatchRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(DefaultNoBumpRegexPattern, Options)]
        public static partial Regex DefaultNoBumpRegex { get; }
#else
        [GeneratedRegex(DefaultNoBumpRegexPattern, Options)]
        private static partial Regex DefaultNoBumpRegexImpl();

        public static Regex DefaultNoBumpRegex { get; } = DefaultNoBumpRegexImpl();
#endif
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
#if NET9_0_OR_GREATER
        [GeneratedRegex(ParseStrictRegexPattern, Options)]
        public static partial Regex ParseStrictRegex { get; }
#else
        [GeneratedRegex(ParseStrictRegexPattern, Options)]
        private static partial Regex ParseStrictRegexImpl();

        public static Regex ParseStrictRegex { get; } = ParseStrictRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(ParseLooseRegexPattern, Options)]
        public static partial Regex ParseLooseRegex { get; }
#else
        [GeneratedRegex(ParseLooseRegexPattern, Options)]
        private static partial Regex ParseLooseRegexImpl();

        public static Regex ParseLooseRegex { get; } = ParseLooseRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(ParseBuildMetaDataRegexPattern, Options)]
        public static partial Regex ParseBuildMetaDataRegex { get; }
#else
        [GeneratedRegex(ParseBuildMetaDataRegexPattern, Options)]
        private static partial Regex ParseBuildMetaDataRegexImpl();

        public static Regex ParseBuildMetaDataRegex { get; } = ParseBuildMetaDataRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(FormatBuildMetaDataRegexPattern, Options)]
        public static partial Regex FormatBuildMetaDataRegex { get; }
#else
        [GeneratedRegex(FormatBuildMetaDataRegexPattern, Options)]
        private static partial Regex FormatBuildMetaDataRegexImpl();

        public static Regex FormatBuildMetaDataRegex { get; } = FormatBuildMetaDataRegexImpl();
#endif

#if NET9_0_OR_GREATER
        [GeneratedRegex(ParsePreReleaseTagRegexPattern, Options)]
        public static partial Regex ParsePreReleaseTagRegex { get; }
#else
        [GeneratedRegex(ParsePreReleaseTagRegexPattern, Options)]
        private static partial Regex ParsePreReleaseTagRegexImpl();

        public static Regex ParsePreReleaseTagRegex { get; } = ParsePreReleaseTagRegexImpl();
#endif
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

#if NET9_0_OR_GREATER
            [GeneratedRegex(TriviaRegexPattern, RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | Options)]
            public static partial Regex TriviaRegex { get; }
#else
            [GeneratedRegex(TriviaRegexPattern, RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | Options)]
            private static partial Regex TriviaRegexImpl();

            public static Regex TriviaRegex { get; } = TriviaRegexImpl();
#endif

#if NET9_0_OR_GREATER
            [GeneratedRegex(AttributeRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
            public static partial Regex AttributeRegex { get; }
#else
            [GeneratedRegex(AttributeRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
            private static partial Regex AttributeRegexImpl();

            public static Regex AttributeRegex { get; } = AttributeRegexImpl();
#endif
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
#if NET9_0_OR_GREATER
            [GeneratedRegex(TriviaRegexPattern, RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | Options)]
            public static partial Regex TriviaRegex { get; }
#else
            [GeneratedRegex(TriviaRegexPattern, RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | Options)]
            private static partial Regex TriviaRegexImpl();

            public static Regex TriviaRegex { get; } = TriviaRegexImpl();
#endif

#if NET9_0_OR_GREATER
            [GeneratedRegex(AttributeRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
            public static partial Regex AttributeRegex { get; }
#else
            [GeneratedRegex(AttributeRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
            private static partial Regex AttributeRegexImpl();

            public static Regex AttributeRegex { get; } = AttributeRegexImpl();
#endif
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

#if NET9_0_OR_GREATER
            [GeneratedRegex(TriviaRegexPattern, RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | Options)]
            public static partial Regex TriviaRegex { get; }
#else
            [GeneratedRegex(TriviaRegexPattern, RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | Options)]
            private static partial Regex TriviaRegexImpl();

            public static Regex TriviaRegex { get; } = TriviaRegexImpl();
#endif

#if NET9_0_OR_GREATER
            [GeneratedRegex(AttributeRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
            public static partial Regex AttributeRegex { get; }
#else
            [GeneratedRegex(AttributeRegexPattern, RegexOptions.IgnorePatternWhitespace | Options)]
            private static partial Regex AttributeRegexImpl();

            public static Regex AttributeRegex { get; } = AttributeRegexImpl();
#endif
        }
    }
}
