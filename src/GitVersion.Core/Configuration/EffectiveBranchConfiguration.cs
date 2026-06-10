using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.Configuration;

/// <summary>Associates an <see cref="EffectiveConfiguration"/> with the branch it was resolved for.</summary>
public record EffectiveBranchConfiguration(EffectiveConfiguration Value, IBranch Branch)
{
    /// <summary>Gets the branch this configuration applies to.</summary>
    public IBranch Branch { get; } = Branch.NotNull();

    /// <summary>Gets the resolved effective configuration for this branch.</summary>
    public EffectiveConfiguration Value { get; } = Value.NotNull();
}
