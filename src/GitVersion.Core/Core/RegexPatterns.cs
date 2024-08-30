using System.Text.RegularExpressions;

namespace GitVersion.Core;

internal static class RegexPatterns
{
    internal static class Common
    {
        public static readonly Regex SwitchArgumentRegex = new(@"/\w+:", RegexOptions.Compiled);
        public static readonly Regex ObscurePasswordRegex = new("(https?://)(.+)(:.+@)", RegexOptions.Compiled);

        // This regex matches an expression to replace.
        // - env:ENV name OR a member name
        // - optional fallback value after " ?? "
        // - the fallback value should be a quoted string, but simple unquoted text is allowed for back compat
        public static readonly Regex ExpandTokensRegex = new("""{((env:(?<envvar>\w+))|(?<member>\w+))(\s+(\?\?)??\s+((?<fallback>\w+)|"(?<fallback>.*)"))??}""", RegexOptions.Compiled);
    }

    internal static class MergeMessage
    {
        public static readonly Regex DefaultMergeMessageRegex = new(@"^Merge (branch|tag) '(?<SourceBranch>[^']*)'(?: into (?<TargetBranch>[^\s]*))*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex SmartGitMergeMessageRegex = new(@"^Finish (?<SourceBranch>[^\s]*)(?: into (?<TargetBranch>[^\s]*))*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex BitBucketPullMergeMessageRegex = new(@"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?<Source>.*) from (?<SourceBranch>[^\s]*) to (?<TargetBranch>[^\s]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex BitBucketPullv7MergeMessageRegex = new(@"^Pull request #(?<PullRequestNumber>\d+).*\r?\n\r?\nMerge in (?<Source>.*) from (?<SourceBranch>[^\s]*) to (?<TargetBranch>[^\s]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex BitBucketCloudPullMergeMessageRegex = new(@"^Merged in (?<SourceBranch>[^\s]*) \(pull request #(?<PullRequestNumber>\d+)\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex GitHubPullMergeMessageRegex = new(@"^Merge pull request #(?<PullRequestNumber>\d+) (from|in) (?:[^\s\/]+\/)?(?<SourceBranch>[^\s]*)(?: into (?<TargetBranch>[^\s]*))*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex RemoteTrackingMergeMessageRegex = new(@"^Merge remote-tracking branch '(?<SourceBranch>[^\s]*)'(?: into (?<TargetBranch>[^\s]*))*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex AzureDevOpsPullMergeMessageRegex = new(@"^Merge pull request (?<PullRequestNumber>\d+) from (?<SourceBranch>[^\s]*) into (?<TargetBranch>[^\s]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    internal static class Output
    {
        public static readonly Regex AssemblyVersionRegex = new(@"AssemblyVersion(Attribute)?\s*\(.*\)\s*");
        public static readonly Regex AssemblyInfoVersionRegex = new(@"AssemblyInformationalVersion(Attribute)?\s*\(.*\)\s*");
        public static readonly Regex AssemblyFileVersionRegex = new(@"AssemblyFileVersion(Attribute)?\s*\(.*\)\s*");
        public static readonly Regex CsharpAssemblyAttributeRegex = new(@"(\s*\[\s*assembly:\s*(?:.*)\s*\]\s*$(\r?\n)?)", RegexOptions.Multiline);
        public static readonly Regex FsharpAssemblyAttributeRegex = new(@"(\s*\[\s*\<assembly:\s*(?:.*)\>\s*\]\s*$(\r?\n)?)", RegexOptions.Multiline);
        public static readonly Regex VisualBasicAssemblyAttributeRegex = new(@"(\s*\<Assembly:\s*(?:.*)\>\s*$(\r?\n)?)", RegexOptions.Multiline);
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

        public static readonly Regex DefaultMajorPatternRegex = new(DefaultMajorPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex DefaultMinorPatternRegex = new(DefaultMinorPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex DefaultPatchPatternRegex = new(DefaultPatchPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex DefaultNoBumpPatternRegex = new(DefaultNoBumpPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    internal static class SemanticVersion
    {
        // uses the git-semver spec https://github.com/semver/semver/blob/master/semver.md
        public static readonly Regex ParseStrictRegex = new(
            @"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex ParseLooseRegex = new(
            @"^(?<SemVer>(?<Major>\d+)(\.(?<Minor>\d+))?(\.(?<Patch>\d+))?)(\.(?<FourthPart>\d+))?(-(?<Tag>[^\+]*))?(\+(?<BuildMetaData>.*))?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex ParseBuildMetaDataRegex = new(
            @"(?<BuildNumber>\d+)?(\.?Branch(Name)?\.(?<BranchName>[^\.]+))?(\.?Sha?\.(?<Sha>[^\.]+))?(?<Other>.*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex FormatBuildMetaDataRegex = new("[^0-9A-Za-z-.]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex ParsePreReleaseTagRegex = new(
            @"(?<name>.*?)\.?(?<number>\d+)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }
}
