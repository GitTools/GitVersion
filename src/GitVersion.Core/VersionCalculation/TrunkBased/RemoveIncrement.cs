namespace GitVersion.VersionCalculation.TrunkBased;

internal sealed class RemoveIncrement : ITrunkBasedContextPostEnricher
{
    public void Enrich(TrunkBasedIteration iteration, TrunkBasedCommit commit, TrunkBasedContext context)
    {
        if (commit.GetEffectiveConfiguration(context.Configuration).IsMainBranch)
        {
            context.Increment = VersionField.None;
            context.Label = null;
            context.AlternativeSemanticVersions.Clear();
        }
    }
}
