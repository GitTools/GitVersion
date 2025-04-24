namespace GitVersion.VersionCalculation.Mainline;

internal sealed class RemoveIncrement : IContextPostEnricher
{
    public void Enrich(MainlineCommit commit, MainlineContext context)
    {
        if (!commit.GetEffectiveConfiguration(context.Configuration).IsMainBranch) return;
        context.Increment = VersionField.None;
        context.Label = null;
        context.AlternativeSemanticVersions.Clear();
    }
}
