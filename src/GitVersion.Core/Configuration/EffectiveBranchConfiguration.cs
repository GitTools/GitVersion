using GitVersion.Extensions;
using GitVersion.Git;

namespace GitVersion.Configuration;

public record EffectiveBranchConfiguration(EffectiveConfiguration Value, IBranch Branch)
{
    public IBranch Branch { get; } = Branch.NotNull();

    public EffectiveConfiguration Value { get; } = Value.NotNull();
}
