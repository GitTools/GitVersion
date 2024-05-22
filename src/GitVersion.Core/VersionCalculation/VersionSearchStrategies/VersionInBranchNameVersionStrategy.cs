using System.Diagnostics.CodeAnalysis;
using GitVersion.Configuration;
using GitVersion.Extensions;

namespace GitVersion.VersionCalculation;

/// <summary>
/// Version is extracted from the name of the branch.
/// BaseVersionSource is the commit where the branch was branched from its parent.
/// Does not increment.
/// </summary>
internal sealed class VersionInBranchNameVersionStrategy(Lazy<GitVersionContext> contextLazy) : IVersionStrategy
{
    private readonly Lazy<GitVersionContext> contextLazy = contextLazy.NotNull();

    private GitVersionContext Context => contextLazy.Value;

    public IEnumerable<BaseVersion> GetBaseVersions(EffectiveBranchConfiguration configuration)
    {
        if (!Context.Configuration.VersionStrategy.HasFlag(VersionStrategies.VersionInBranchName))
            yield break;

        if (TryGetBaseVersion(configuration, out var baseVersion))
        {
            yield return baseVersion;
        }
    }

    public bool TryGetBaseVersion(EffectiveBranchConfiguration configuration, [NotNullWhen(true)] out BaseVersion? baseVersion)
    {
        baseVersion = null;

        if (!configuration.Value.IsReleaseBranch)
            return false;

        foreach (var branch in new[] { Context.CurrentBranch, configuration.Branch })
        {
            if (branch.Name.TryGetSemanticVersion(out var result, configuration.Value.VersionInBranchRegex,
                configuration.Value.TagPrefix, configuration.Value.SemanticVersionFormat))
            {
                string? branchNameOverride = null;
                if (!result.Name.IsNullOrEmpty() && (Context.CurrentBranch.Name.Equals(branch.Name)
                    || Context.Configuration.GetBranchConfiguration(Context.CurrentBranch.Name).Label is null))
                {
                    branchNameOverride = result.Name;
                }

                var label = configuration.Value.GetBranchSpecificLabel(Context.CurrentBranch.Name, branchNameOverride);

                baseVersion = new BaseVersion("Version in branch name", result.Value)
                {
                    Operator = new BaseVersionOperator()
                    {
                        Increment = VersionField.None,
                        ForceIncrement = false,
                        Label = label
                    }
                };
                return true;
            }
        }

        return false;
    }
}
