namespace GitVersion.VersionCalculation.Mainline;

internal sealed class EnrichMergeState : IContextPreEnricher
{
    public void Enrich(MainlineIteration iteration, MainlineCommit commit, MainlineContext context)
    {
        context.IsMainlineMerge = context.SemanticVersion is null && context.IsMergedMainline(commit);
        if (commit.HasUnsupportedMerge
            && (!commit.HasExplicitIncrement || context.SemanticVersion is null))
        {
            throw new NotSupportedException(
                "GitVersion does not support more than one merge source in a single commit yet");
        }
    }
}
