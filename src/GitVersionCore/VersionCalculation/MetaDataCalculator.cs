namespace GitVersion.VersionCalculation
{
    public class MetaDataCalculator : IMetaDataCalculator
    {
        public SemanticVersionBuildMetaData Create(int commitCount, GitVersionContext context)
        {
            return new SemanticVersionBuildMetaData(
                commitCount,
                context.CurrentBranch.FriendlyName,
                context.CurrentCommit.Sha,
                context.CurrentCommit.When());
        }
    }
}