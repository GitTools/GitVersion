namespace GitVersion.VersionCalculation
{
    using System;
    using System.Text.RegularExpressions;
    using BaseVersionCalculators;

    public class NewNextVersionCalculator
    {
        IBaseVersionCalculator baseVersionFinder;
        IMetaDataCalculator metaDataCalculator;

        public NewNextVersionCalculator(IBaseVersionCalculator baseVersionCalculator = null, IMetaDataCalculator metaDataCalculator = null)
        {
            this.metaDataCalculator = metaDataCalculator ?? new MetaDataCalculator();
            baseVersionFinder = baseVersionCalculator ??
                new BaseVersionCalculator(
                new ConfigNextVersionBaseVersionStrategy(),
                new LastTagBaseVersionStrategy(),
                new MergeMessageBaseVersionStrategy(),
                new VersionInBranchBaseVersionStrategy());
        }

        public SemanticVersion FindVersion(GitVersionContext context)
        {
            var baseVersion = baseVersionFinder.GetBaseVersion(context);

            if (baseVersion.ShouldIncrement) IncrementVersion(context, baseVersion);

            if (!baseVersion.SemanticVersion.PreReleaseTag.HasTag() && !string.IsNullOrEmpty(context.Configuration.Tag))
            {
                var tagToUse = context.Configuration.Tag;
                if (tagToUse == "useBranchName")
                    tagToUse = context.CurrentBranch.Name.RegexReplace(context.Configuration.BranchPrefixToTrim, string.Empty, RegexOptions.IgnoreCase);
                baseVersion.SemanticVersion.PreReleaseTag = new SemanticVersionPreReleaseTag(tagToUse, 1);
            }

            baseVersion.SemanticVersion.BuildMetaData = metaDataCalculator.Create(baseVersion.BaseVersionSource, context);

            return baseVersion.SemanticVersion;
        }

        static void IncrementVersion(GitVersionContext context, BaseVersion baseVersion)
        {
            if (!baseVersion.SemanticVersion.PreReleaseTag.HasTag())
            {
                switch (context.Configuration.Increment)
                {
                    case IncrementStrategy.None:
                        break;
                    case IncrementStrategy.Major:
                        baseVersion.SemanticVersion.Major++;
                        break;
                    case IncrementStrategy.Minor:
                        baseVersion.SemanticVersion.Minor++;
                        break;
                    case IncrementStrategy.Patch:
                        baseVersion.SemanticVersion.Patch++;
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