using System.Text.RegularExpressions;
using GitVersion.Configuration;
using GitVersion.Extensions;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.OutputVariables;

namespace GitVersion.VersionCalculation;

public class VariableProvider : IVariableProvider
{
    private readonly IEnvironment environment;
    private readonly ILog log;

    public VariableProvider(IEnvironment environment, ILog log)
    {
        this.environment = environment.NotNull();
        this.log = log.NotNull();
    }

    public VersionVariables GetVariablesFor(SemanticVersion semanticVersion, EffectiveConfiguration configuration, bool isCurrentCommitTagged)
    {
        var isContinuousDeploymentMode = configuration.VersioningMode == VersioningMode.ContinuousDeployment && !isCurrentCommitTagged;
        if (isContinuousDeploymentMode)
        {
            semanticVersion = new SemanticVersion(semanticVersion);
            // Continuous Deployment always requires a pre-release tag unless the commit is tagged
            if (semanticVersion.PreReleaseTag != null && semanticVersion.PreReleaseTag.HasTag() != true)
            {
                semanticVersion.PreReleaseTag.Name = configuration.GetBranchSpecificTag(this.log, semanticVersion.BuildMetaData?.Branch, null);
                if (semanticVersion.PreReleaseTag.Name.IsNullOrEmpty())
                {
                    // TODO: Why do we manipulating the semantic version here in the VariableProvider? The method name is GET not MANIPULATE.
                    // What is about the separation of concern and single-responsibility principle?
                    semanticVersion.PreReleaseTag.Name = configuration.ContinuousDeploymentFallbackTag;
                }
            }
        }

        // Evaluate tag number pattern and append to prerelease tag, preserving build metadata
        var appendTagNumberPattern = !configuration.TagNumberPattern.IsNullOrEmpty() && semanticVersion.PreReleaseTag?.HasTag() == true;
        if (appendTagNumberPattern)
        {
            if (semanticVersion.BuildMetaData?.Branch != null && configuration.TagNumberPattern != null)
            {
                var match = Regex.Match(semanticVersion.BuildMetaData.Branch, configuration.TagNumberPattern);
                var numberGroup = match.Groups["number"];
                if (numberGroup.Success && semanticVersion.PreReleaseTag != null)
                {
                    // TODO: Why do we manipulating the semantic version here in the VariableProvider? The method name is GET not MANIPULATE.
                    // What is about the separation of concern and single-responsibility principle?
                    semanticVersion.PreReleaseTag.Name += numberGroup.Value;
                }
            }
        }

        if (isContinuousDeploymentMode || appendTagNumberPattern || configuration.VersioningMode == VersioningMode.Mainline)
        {
            // TODO: Why do we manipulating the semantic version here in the VariableProvider? The method name is GET not MANIPULATE.
            // What is about the separation of concern and single-responsibility principle?
            PromoteNumberOfCommitsToTagNumber(semanticVersion);
        }

        var semverFormatValues = new SemanticVersionFormatValues(semanticVersion, configuration);

        var informationalVersion = CheckAndFormatString(configuration.AssemblyInformationalFormat, semverFormatValues, semverFormatValues.InformationalVersion, "AssemblyInformationalVersion");

        var assemblyFileSemVer = CheckAndFormatString(configuration.AssemblyFileVersioningFormat, semverFormatValues, semverFormatValues.AssemblyFileSemVer, "AssemblyFileVersioningFormat");

        var assemblySemVer = CheckAndFormatString(configuration.AssemblyVersioningFormat, semverFormatValues, semverFormatValues.AssemblySemVer, "AssemblyVersioningFormat");

        var variables = new VersionVariables(
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
            semverFormatValues.UncommittedChanges);

        return variables;
    }

    private static void PromoteNumberOfCommitsToTagNumber(SemanticVersion semanticVersion)
    {
        if (semanticVersion.PreReleaseTag != null && semanticVersion.BuildMetaData != null)
        {
            // For continuous deployment the commits since tag gets promoted to the pre-release number
            if (!semanticVersion.BuildMetaData.CommitsSinceTag.HasValue)
            {
                semanticVersion.PreReleaseTag.Number = null;
                semanticVersion.BuildMetaData.CommitsSinceVersionSource = 0;
            }
            else
            {
                // Number of commits since last tag should be added to PreRelease number if given. Remember to deduct automatic version bump.
                if (semanticVersion.PreReleaseTag.Number.HasValue)
                {
                    semanticVersion.PreReleaseTag.Number += semanticVersion.BuildMetaData.CommitsSinceTag - 1;
                }
                else
                {
                    semanticVersion.PreReleaseTag.Number = semanticVersion.BuildMetaData.CommitsSinceTag;
                    semanticVersion.PreReleaseTag.PromotedFromCommits = true;
                }
                semanticVersion.BuildMetaData.CommitsSinceVersionSource = semanticVersion.BuildMetaData.CommitsSinceTag.Value;
                semanticVersion.BuildMetaData.CommitsSinceTag = null; // why is this set to null ?
            }
        }
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
