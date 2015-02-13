namespace GitVersion.VersionCalculation
{
    using System;
    using System.Text.RegularExpressions;
    using BaseVersionCalculators;

    public class NewNextVersionCalculator
    {
        IBaseVersionCalculator baseVersionFinder;
        IMetaDataCalculator metaDataCalculator;
        LastTagBaseVersionStrategy lastTagBaseVersionStrategy;

        public NewNextVersionCalculator(IBaseVersionCalculator baseVersionCalculator = null, IMetaDataCalculator metaDataCalculator = null)
        {
            this.metaDataCalculator = metaDataCalculator ?? new MetaDataCalculator();
            lastTagBaseVersionStrategy = new LastTagBaseVersionStrategy();
            baseVersionFinder = baseVersionCalculator ??
                new BaseVersionCalculator(
                    new FallbackBaseVersionStrategy(),
                new ConfigNextVersionBaseVersionStrategy(),
                lastTagBaseVersionStrategy,
                new MergeMessageBaseVersionStrategy(),
                new VersionInBranchBaseVersionStrategy());
        }

        public SemanticVersion FindVersion(GitVersionContext context)
        {
            var baseVersion = baseVersionFinder.GetBaseVersion(context);

            if (baseVersion.ShouldIncrement) IncrementVersion(context, baseVersion);
            else Logger.WriteInfo("Skipping version increment");

            if (baseVersion.ShouldUpdateTag && !baseVersion.SemanticVersion.PreReleaseTag.HasTag() && !string.IsNullOrEmpty(context.Configuration.Tag))
            {
                var tagToUse = context.Configuration.Tag;
                if (tagToUse == "useBranchName")
                {
                    Logger.WriteInfo("Using branch name to calculate version tag");
                    var name = baseVersion.BranchNameOverride ?? context.CurrentBranch.Name;
                    tagToUse = name.RegexReplace(context.Configuration.BranchPrefixToTrim, string.Empty, RegexOptions.IgnoreCase);
                }
                int? number = null;
                if (!string.IsNullOrEmpty(context.Configuration.TagNumberPattern))
                {
                    var match = Regex.Match(context.CurrentBranch.CanonicalName, context.Configuration.TagNumberPattern);
                    var numberGroup = match.Groups["number"];
                    if (numberGroup.Success)
                        number = int.Parse(numberGroup.Value);
                }

                var lastTag = lastTagBaseVersionStrategy.GetVersion(context);
                if (number == null &&
                    lastTag != null &&
                    !context.IsCurrentCommitTagged &&
                    MajorMinorPatchEqual(lastTag.SemanticVersion, baseVersion.SemanticVersion) &&
                    lastTag.SemanticVersion.PreReleaseTag.HasTag())
                {
                    number = lastTag.SemanticVersion.PreReleaseTag.Number + 1;
                }

                if (number == null)
                    number = 1;

                baseVersion.SemanticVersion.PreReleaseTag = new SemanticVersionPreReleaseTag(tagToUse, number);
            }

            baseVersion.SemanticVersion.BuildMetaData = metaDataCalculator.Create(baseVersion.BaseVersionSource, context);

            return baseVersion.SemanticVersion;
        }

        static bool MajorMinorPatchEqual(SemanticVersion lastTag, SemanticVersion baseVersion)
        {
            return lastTag.Major == baseVersion.Major &&
                   lastTag.Minor == baseVersion.Minor &&
                   lastTag.Patch == baseVersion.Patch;
        }

        static void IncrementVersion(GitVersionContext context, BaseVersion baseVersion)
        {
            if (!baseVersion.SemanticVersion.PreReleaseTag.HasTag())
            {
                switch (context.Configuration.Increment)
                {
                    case IncrementStrategy.None:
                        Logger.WriteInfo("Skipping version increment");
                        break;
                    case IncrementStrategy.Major:
                        Logger.WriteInfo("Incrementing Major Version");
                        baseVersion.SemanticVersion.Major++;
                        baseVersion.SemanticVersion.Minor = 0;
                        baseVersion.SemanticVersion.Patch = 0;
                        break;
                    case IncrementStrategy.Minor:
                        baseVersion.SemanticVersion.Minor++;
                        baseVersion.SemanticVersion.Patch = 0;
                        Logger.WriteInfo("Incrementing Minor Version");
                        break;
                    case IncrementStrategy.Patch:
                        baseVersion.SemanticVersion.Patch++;
                        Logger.WriteInfo("Incrementing Patch Version");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                if (baseVersion.SemanticVersion.PreReleaseTag.Number != null)
                {
                    baseVersion.SemanticVersion.PreReleaseTag.Number = baseVersion.SemanticVersion.PreReleaseTag.Number;
                    baseVersion.SemanticVersion.PreReleaseTag.Number++;
                }
            }
        }
    }
}