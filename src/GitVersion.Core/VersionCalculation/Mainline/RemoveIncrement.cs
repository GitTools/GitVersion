namespace GitVersion.VersionCalculation.Mainline;

internal sealed class RemoveIncrement : IContextPostEnricher
{
    public void Enrich(MainlineCommit commit, MainlineContext context)
    {
        if (!context.GetEffectiveConfiguration(commit).IsMainBranch)
        {
            return;
        }

        context.SuppressBranchIncrement = false;
        context.Increment = VersionField.None;
        context.Label = null;
        context.ClearAlternativeSemanticVersions();
    }
}
