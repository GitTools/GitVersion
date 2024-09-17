using System.Text.RegularExpressions;

namespace GitVersion.Core;

internal static class RegexPatterns
{
    internal static class Common
    {
        public static Regex SwitchArgumentRegex { get; } = new(@"/\w+:", RegexOptions.Compiled);
        public static Regex ObscurePasswordRegex { get; } = new("(https?://)(.+)(:.+@)", RegexOptions.Compiled);

        // This regex matches an expression to replace.
        // - env:ENV name OR a member name
        // - optional fallback value after " ?? "
        // - the fallback value should be a quoted string, but simple unquoted text is allowed for back compat
        public static Regex ExpandTokensRegex { get; } = new("""{((env:(?<envvar>\w+))|(?<member>\w+))(\s+(\?\?)??\s+((?<fallback>\w+)|"(?<fallback>.*)"))??}""", RegexOptions.Compiled);
    }

    internal static class Configuration
    {
        //language=regexp
        public const string DefaultTagPrefixPattern = "[vV]?";

        //language=regexp
        public const string DefaultVersionInBranchPattern = @"(?<version>[vV]?\d+(\.\d+)?(\.\d+)?).*";

        //language=regexp
        public const string DefaultLabelNumberPattern = @"[/-](?<number>\d+)";

        //language=regexp
        public const string MainBranchRegexPattern = "^master$|^main$";

        //language=regexp
        public const string DevelopBranchRegexPattern = "^dev(elop)?(ment)?$";

        //language=regexp
        public const string ReleaseBranchRegexPattern = "^releases?[/-](?<BranchName>.+)";

        //language=regexp
        public const string FeatureBranchRegexPattern = "^features?[/-](?<BranchName>.+)";

        //language=regexp
        public const string PullRequestBranchRegexPattern = @"^(pull|pull\-requests|pr)[/-]";

        //language=regexp
        public const string HotfixBranchRegexPattern = "^hotfix(es)?[/-](?<BranchName>.+)";

        //language=regexp
        public const string SupportBranchRegexPattern = "^support[/-](?<BranchName>.+)";

        //language=regexp
        public const string UnknownBranchRegexPattern = "(?<BranchName>.+)";
    }

    internal static class MergeMessage
    {
        public static Regex DefaultMergeMessageRegex { get; } = new(@"^Merge (branch|tag) '(?<SourceBranch>[^']*)'(?: into (?<TargetBranch>[^\s]*))*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex SmartGitMergeMessageRegex { get; } = new(@"^Finish (?<SourceBranch>[^\s]*)(?: into (?<TargetBranch>[^\s]*))*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex BitBucketPullMergeMessageRegex { get; } = new(@"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?<Source>.*) from (?<SourceBranch>[^\s]*) to (?<TargetBranch>[^\s]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex BitBucketPullv7MergeMessageRegex { get; } = new(@"^Pull request #(?<PullRequestNumber>\d+).*\r?\n\r?\nMerge in (?<Source>.*) from (?<SourceBranch>[^\s]*) to (?<TargetBranch>[^\s]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex BitBucketCloudPullMergeMessageRegex { get; } = new(@"^Merged in (?<SourceBranch>[^\s]*) \(pull request #(?<PullRequestNumber>\d+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex GitHubPullMergeMessageRegex { get; } = new(@"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?:[^\s\/]+\/)?(?<SourceBranch>[^\s]*)(?: into (?<TargetBranch>[^\s]*))*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex RemoteTrackingMergeMessageRegex { get; } = new(@"^Merge remote-tracking branch '(?<SourceBranch>[^\s]*)'(?: into (?<TargetBranch>[^\s]*))*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex AzureDevOpsPullMergeMessageRegex { get; } = new(@"^Merge pull request (?<PullRequestNumber>\d+) from (?<SourceBranch>[^\s]*) into (?<TargetBranch>[^\s]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    internal static class Output
    {
        public static Regex AssemblyVersionRegex { get; } = new(@"AssemblyVersion(Attribute)?\s*\(.*\)\s*");
        public static Regex AssemblyInfoVersionRegex { get; } = new(@"AssemblyInformationalVersion(Attribute)?\s*\(.*\)\s*");
        public static Regex AssemblyFileVersionRegex { get; } = new(@"AssemblyFileVersion(Attribute)?\s*\(.*\)\s*");
        public static Regex CsharpAssemblyAttributeRegex { get; } = new(@"(\s*\[\s*assembly:\s*(?:.*)\s*\]\s*$(\r?\n)?)", RegexOptions.Multiline);
        public static Regex FsharpAssemblyAttributeRegex { get; } = new(@"(\s*\[\s*\<assembly:\s*(?:.*)\>\s*\]\s*$(\r?\n)?)", RegexOptions.Multiline);
        public static Regex VisualBasicAssemblyAttributeRegex { get; } = new(@"(\s*\<Assembly:\s*(?:.*)\>\s*$(\r?\n)?)", RegexOptions.Multiline);
    }

    internal static class VersionCalculation
    {
        //language=regexp
        public const string DefaultMajorPattern = @"\+semver:\s?(breaking|major)";

        //language=regexp
        public const string DefaultMinorPattern = @"\+semver:\s?(feature|minor)";

        //language=regexp
        public const string DefaultPatchPattern = @"\+semver:\s?(fix|patch)";

        //language=regexp
        public const string DefaultNoBumpPattern = @"\+semver:\s?(none|skip)";

        public static Regex DefaultMajorRegex { get; } = new(DefaultMajorPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex DefaultMinorRegex { get; } = new(DefaultMinorPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex DefaultPatchRegex { get; } = new(DefaultPatchPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex DefaultNoBumpRegex { get; } = new(DefaultNoBumpPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    internal static class SemanticVersion
    {
        // uses the git-semver spec https://github.com/semver/semver/blob/master/semver.md
        public static Regex ParseStrictRegex { get; } = new(
            @"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static Regex ParseLooseRegex { get; } = new(
            @"^(?<SemVer>(?<Major>\d+)(\.(?<Minor>\d+))?(\.(?<Patch>\d+))?)(\.(?<FourthPart>\d+))?(-(?<Tag>[^\+]*))?(\+(?<BuildMetaData>.*))?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static Regex ParseBuildMetaDataRegex { get; } = new(
            @"(?<BuildNumber>\d+)?(\.?Branch(Name)?\.(?<BranchName>[^\.]+))?(\.?Sha?\.(?<Sha>[^\.]+))?(?<Other>.*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static Regex FormatBuildMetaDataRegex { get; } = new("[^0-9A-Za-z-.]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static Regex ParsePreReleaseTagRegex { get; } = new(
            @"(?<name>.*?)\.?(?<number>\d+)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }
}
