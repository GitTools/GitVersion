using System.Text.RegularExpressions;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.OutputVariables;

namespace GitVersion.VersionCalculation;

public class VariableProvider : IVariableProvider
{
    private readonly IEnvironment environment;

    public VariableProvider(IEnvironment environment) => this.environment = environment.NotNull();

    public VersionVariables GetVariablesFor(
        SemanticVersion semanticVersion, EffectiveConfiguration configuration, SemanticVersion? currentCommitTaggedVersion)
    {
        var preReleaseTagName = semanticVersion.PreReleaseTag.Name;

        var isContinuousDeploymentMode = configuration.VersioningMode == VersioningMode.ContinuousDeployment
            && currentCommitTaggedVersion is null;
        if (isContinuousDeploymentMode)
        {
            semanticVersion = new SemanticVersion(semanticVersion);
            // Continuous Deployment always requires a pre-release tag unless the commit is tagged
            if (!semanticVersion.PreReleaseTag.HasTag())
            {
                // TODO: Why do we manipulating the semantic version here in the VariableProvider? The method name is GET not MANIPULATE.
                // What is about the separation of concern and single-responsibility principle?
                preReleaseTagName = configuration.GetBranchSpecificLabel(semanticVersion.BuildMetaData.Branch, null);
                if (preReleaseTagName.IsNullOrEmpty())
                {
                    preReleaseTagName = configuration.Label ?? string.Empty;
                }
            }
        }

        // Evaluate tag number pattern and append to prerelease tag, preserving build metadata
        var appendTagNumberPattern = !configuration.LabelNumberPattern.IsNullOrEmpty() && semanticVersion.PreReleaseTag.HasTag();
        if (appendTagNumberPattern && semanticVersion.BuildMetaData.Branch != null && configuration.LabelNumberPattern != null)
        {
            var match = Regex.Match(semanticVersion.BuildMetaData.Branch, configuration.LabelNumberPattern);
            var numberGroup = match.Groups["number"];
            if (numberGroup.Success)
            {
                // TODO: Why do we manipulating the semantic version here in the VariableProvider? The method name is GET not MANIPULATE.
                // What is about the separation of concern and single-responsibility principle?
                preReleaseTagName += numberGroup.Value;
            }
        }

        if (isContinuousDeploymentMode || appendTagNumberPattern || configuration.VersioningMode == VersioningMode.Mainline)
        {
            // TODO: Why do we manipulating the semantic version here in the VariableProvider? The method name is GET not MANIPULATE.
            // What is about the separation of concern and single-responsibility principle?
            semanticVersion = PromoteNumberOfCommitsToTagNumber(semanticVersion, preReleaseTagName);
        }
        else
        {
            semanticVersion = new(semanticVersion)
            {
                PreReleaseTag = new(semanticVersion.PreReleaseTag)
                {
                    Name = preReleaseTagName
                }
            };
        }

        if (semanticVersion.CompareTo(currentCommitTaggedVersion) == 0)
        {
            // Will always be 0, don't bother with the +0 on tags
            semanticVersion = new(semanticVersion)
            {
                BuildMetaData = new(semanticVersion.BuildMetaData)
                {
                    CommitsSinceTag = null
                }
            };
        }

        var semverFormatValues = new SemanticVersionFormatValues(semanticVersion, configuration);

        var informationalVersion = CheckAndFormatString(configuration.AssemblyInformationalFormat, semverFormatValues, semverFormatValues.InformationalVersion, "AssemblyInformationalVersion");

        var assemblyFileSemVer = CheckAndFormatString(configuration.AssemblyFileVersioningFormat, semverFormatValues, semverFormatValues.AssemblyFileSemVer, "AssemblyFileVersioningFormat");

        var assemblySemVer = CheckAndFormatString(configuration.AssemblyVersioningFormat, semverFormatValues, semverFormatValues.AssemblySemVer, "AssemblyVersioningFormat");

        return new VersionVariables(
            semverFormatValues.Major,
            semverFormatValues.Minor,
            semverFormatValues.Patch,
            semverFormatValues.BuildMetaData,
            semverFormatValues.FullBuildMetaData,
            semverFormatValues.BranchName,
            semverFormatValues.EscapedBranchName,
            semverFormatValues.Sha,
            semverFormatValues.ShortSha,
            semverFormatValues.MajorMinorPatch,
            semverFormatValues.SemVer,
            semverFormatValues.FullSemVer,
            assemblySemVer,
            assemblyFileSemVer,
            semverFormatValues.PreReleaseTag,
            semverFormatValues.PreReleaseTagWithDash,
            semverFormatValues.PreReleaseLabel,
            semverFormatValues.PreReleaseLabelWithDash,
            semverFormatValues.PreReleaseNumber,
            semverFormatValues.WeightedPreReleaseNumber,
            informationalVersion,
            semverFormatValues.CommitDate,
            semverFormatValues.VersionSourceSha,
            semverFormatValues.CommitsSinceVersionSource,
            semverFormatValues.UncommittedChanges
        );
    }

    private static SemanticVersion PromoteNumberOfCommitsToTagNumber(SemanticVersion semanticVersion, string preReleaseTagName)
    {
        var preReleaseTagNumber = semanticVersion.PreReleaseTag.Number;
        var preReleaseTagPromotedFromCommits = semanticVersion.PreReleaseTag.PromotedFromCommits;
        long buildMetaDataCommitsSinceVersionSource;
        var buildMetaDataCommitsSinceTag = semanticVersion.BuildMetaData.CommitsSinceTag;

        // For continuous deployment the commits since tag gets promoted to the pre-release number
        if (!semanticVersion.BuildMetaData.CommitsSinceTag.HasValue)
        {
            preReleaseTagNumber = null;
            buildMetaDataCommitsSinceVersionSource = 0;
        }
        else
        {
            // Number of commits since last tag should be added to PreRelease number if given. Remember to deduct automatic version bump.
            if (preReleaseTagNumber.HasValue)
            {
                preReleaseTagNumber += semanticVersion.BuildMetaData.CommitsSinceTag - 1;
            }
            else
            {
                preReleaseTagNumber = semanticVersion.BuildMetaData.CommitsSinceTag;
                preReleaseTagPromotedFromCommits = true;
            }
            buildMetaDataCommitsSinceVersionSource = semanticVersion.BuildMetaData.CommitsSinceTag.Value;
            buildMetaDataCommitsSinceTag = null; // why is this set to null ?
        }

        return new SemanticVersion(semanticVersion)
        {
            PreReleaseTag = new(semanticVersion.PreReleaseTag)
            {
                Name = preReleaseTagName,
                Number = preReleaseTagNumber,
                PromotedFromCommits = preReleaseTagPromotedFromCommits
            },
            BuildMetaData = new(semanticVersion.BuildMetaData)
            {
                CommitsSinceVersionSource = buildMetaDataCommitsSinceVersionSource,
                CommitsSinceTag = buildMetaDataCommitsSinceTag
            }
        };
    }

    private string? CheckAndFormatString<T>(string? formatString, T source, string? defaultValue, string formatVarName)
    {
        string? formattedString;

        if (formatString.IsNullOrEmpty())
        {
            formattedString = defaultValue;
        }
        else
        {
            try
            {
                formattedString = formatString.FormatWith(source, this.environment).RegexReplace("[^0-9A-Za-z-.+]", "-");
            }
            catch (ArgumentException exception)
            {
                throw new WarningException($"Unable to format {formatVarName}.  Check your format string: {exception.Message}");
            }
        }

        return formattedString;
    }
}
