using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace GitVersion.Core;

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
        Cache.TryAdd(Configuration.DefaultLabelNumberRegex.ToString(), Configuration.DefaultLabelNumberRegex);
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
        public static Regex SwitchArgumentRegex { get; } = new(@"/\w+:", Options);
        public static Regex ObscurePasswordRegex { get; } = new("(https?://)(.+)(:.+@)", Options);

        // This regex matches an expression to replace.
        // - env:ENV name OR a member name
        // - optional fallback value after " ?? "
        // - the fallback value should be a quoted string, but simple unquoted text is allowed for back compat
        public static Regex ExpandTokensRegex { get; } = new("""{((env:(?<envvar>\w+))|(?<member>\w+))(\s+(\?\?)??\s+((?<fallback>\w+)|"(?<fallback>.*)"))??}""", Options);
    }

    internal static class Configuration
    {
        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string DefaultTagPrefixPattern = "[vV]?";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string DefaultVersionInBranchPattern = @"(?<version>[vV]?\d+(\.\d+)?(\.\d+)?).*";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string DefaultLabelNumberPattern = @"[/-](?<number>\d+)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string MainBranchRegexPattern = "^master$|^main$";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string DevelopBranchRegexPattern = "^dev(elop)?(ment)?$";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string ReleaseBranchRegexPattern = "^releases?[/-](?<BranchName>.+)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string FeatureBranchRegexPattern = "^features?[/-](?<BranchName>.+)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string PullRequestBranchRegexPattern = @"^(pull|pull\-requests|pr)[/-]";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string HotfixBranchRegexPattern = "^hotfix(es)?[/-](?<BranchName>.+)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string SupportBranchRegexPattern = "^support[/-](?<BranchName>.+)";

        [StringSyntax(StringSyntaxAttribute.Regex)]
        public const string UnknownBranchRegexPattern = "(?<BranchName>.+)";

        public static Regex DefaultTagPrefixRegex { get; } = new(DefaultTagPrefixPattern, Options);
        public static Regex DefaultVersionInBranchRegex { get; } = new(DefaultVersionInBranchPattern, Options);
        public static Regex DefaultLabelNumberRegex { get; } = new(DefaultLabelNumberPattern, Options);
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
        public static Regex DefaultMergeMessageRegex { get; } = new(@"^Merge (branch|tag) '(?<SourceBranch>[^']*)'(?: into (?<TargetBranch>[^\s]*))*", Options);
        public static Regex SmartGitMergeMessageRegex { get; } = new(@"^Finish (?<SourceBranch>[^\s]*)(?: into (?<TargetBranch>[^\s]*))*", Options);
        public static Regex BitBucketPullMergeMessageRegex { get; } = new(@"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?<Source>.*) from (?<SourceBranch>[^\s]*) to (?<TargetBranch>[^\s]*)", Options);
        public static Regex BitBucketPullv7MergeMessageRegex { get; } = new(@"^Pull request #(?<PullRequestNumber>\d+).*\r?\n\r?\nMerge in (?<Source>.*) from (?<SourceBranch>[^\s]*) to (?<TargetBranch>[^\s]*)", Options);
        public static Regex BitBucketCloudPullMergeMessageRegex { get; } = new(@"^Merged in (?<SourceBranch>[^\s]*) \(pull request #(?<PullRequestNumber>\d+)\)", Options);
        public static Regex GitHubPullMergeMessageRegex { get; } = new(@"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?:[^\s\/]+\/)?(?<SourceBranch>[^\s]*)(?: into (?<TargetBranch>[^\s]*))*", Options);
        public static Regex RemoteTrackingMergeMessageRegex { get; } = new(@"^Merge remote-tracking branch '(?<SourceBranch>[^\s]*)'(?: into (?<TargetBranch>[^\s]*))*", Options);
        public static Regex AzureDevOpsPullMergeMessageRegex { get; } = new(@"^Merge pull request (?<PullRequestNumber>\d+) from (?<SourceBranch>[^\s]*) into (?<TargetBranch>[^\s]*)", Options);
    }

    internal static class Output
    {
        public static Regex AssemblyVersionRegex { get; } = new(@"AssemblyVersion(Attribute)?\s*\(.*\)\s*", Options);
        public static Regex AssemblyInfoVersionRegex { get; } = new(@"AssemblyInformationalVersion(Attribute)?\s*\(.*\)\s*", Options);
        public static Regex AssemblyFileVersionRegex { get; } = new(@"AssemblyFileVersion(Attribute)?\s*\(.*\)\s*", Options);
        public static Regex CsharpAssemblyAttributeRegex { get; } = new(@"(\s*\[\s*assembly:\s*(?:.*)\s*\]\s*$(\r?\n)?)", Options | RegexOptions.Multiline);
        public static Regex FsharpAssemblyAttributeRegex { get; } = new(@"(\s*\[\s*\<assembly:\s*(?:.*)\>\s*\]\s*$(\r?\n)?)", Options | RegexOptions.Multiline);
        public static Regex VisualBasicAssemblyAttributeRegex { get; } = new(@"(\s*\<Assembly:\s*(?:.*)\>\s*$(\r?\n)?)", Options | RegexOptions.Multiline);
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
        // uses the git-semver spec https://github.com/semver/semver/blob/master/semver.md
        public static Regex ParseStrictRegex { get; } = new(
            @"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
            Options);

        public static Regex ParseLooseRegex { get; } = new(
            @"^(?<SemVer>(?<Major>\d+)(\.(?<Minor>\d+))?(\.(?<Patch>\d+))?)(\.(?<FourthPart>\d+))?(-(?<Tag>[^\+]*))?(\+(?<BuildMetaData>.*))?$",
            Options);

        public static Regex ParseBuildMetaDataRegex { get; } = new(
            @"(?<BuildNumber>\d+)?(\.?Branch(Name)?\.(?<BranchName>[^\.]+))?(\.?Sha?\.(?<Sha>[^\.]+))?(?<Other>.*)",
            Options);

        public static Regex FormatBuildMetaDataRegex { get; } = new("[^0-9A-Za-z-.]",
            Options);

        public static Regex ParsePreReleaseTagRegex { get; } = new(
            @"(?<name>.*?)\.?(?<number>\d+)?$",
            Options);
    }

    internal static class AssemblyVersion
    {
        internal static class CSharp
        {
            public static Regex TriviaRegex { get; } = new(@"
    /\*(.*?)\*/                       # Block comments: matches /* ... */
    |//(.*?)\r?\n                     # Line comments: matches // ... followed by a newline
    |""((\\[^\n]|[^""\n])*)""         # Strings: matches "" ... "" including escaped quotes",
                RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | Options);

            public static Regex AttributeRegex { get; } = new(@"(?x) # IgnorePatternWhitespace
\[\s*assembly\s*:\s*                  # The [assembly: part
(System\s*\.\s*Reflection\s*\.\s*)?   # The System.Reflection. part (optional)
Assembly(File|Informational)?Version  # The attribute AssemblyVersion, AssemblyFileVersion, or AssemblyInformationalVersion
\s*\(\s*\)\s*\]                       # End brackets ()]",
                RegexOptions.IgnorePatternWhitespace | Options);
        }

        internal static class FSharp
        {
            public static Regex TriviaRegex { get; } = new(@"
    /\*(.*?)\*/                       # Block comments: matches /* ... */
    |//(.*?)\r?\n                     # Line comments: matches // ... followed by a newline
    |""((\\[^\n]|[^""\n])*)""         # Strings: matches "" ... "" including escaped quotes",
                RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | Options);

            public static Regex AttributeRegex { get; } = new(@"(?x) # IgnorePatternWhitespace
\[\s*<\s*assembly\s*:\s*              # The [<assembly: part
(System\s*\.\s*Reflection\s*\.\s*)?   # The System.Reflection. part (optional)
Assembly(File|Informational)?Version  # The attribute AssemblyVersion, AssemblyFileVersion, or AssemblyInformationalVersion
\s*\(\s*\)\s*>\s*\]                   # End brackets ()>]",
                RegexOptions.IgnorePatternWhitespace | Options);
        }

        internal static class VisualBasic
        {
            public static Regex TriviaRegex { get; } = new(@"
    '(.*?)\r?\n                       # Line comments: matches // ... followed by a newline
    |""((\\[^\n]|[^""\n])*)""         # Strings: matches "" ... "" including escaped quotes",
                RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | Options);

            public static Regex AttributeRegex { get; } = new(@"(?x) # IgnorePatternWhitespace
\<\s*Assembly\s*:\s*                  # The <Assembly: part
(System\s*\.\s*Reflection\s*\.\s*)?   # The System.Reflection. part (optional)
Assembly(File|Informational)?Version  # The attribute AssemblyVersion, AssemblyFileVersion, or AssemblyInformationalVersion
\s*\(\s*\)\s*\>                       # End brackets ()>",
                RegexOptions.IgnorePatternWhitespace | Options);
        }
    }
}
