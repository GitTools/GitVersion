namespace GitFlowVersion
{
    public class ReleaseInformation
    {
        public ReleaseInformation(Stability? stability, int? releaseNumber)
        {
            ReleaseNumber = releaseNumber;
            Stability = stability;
        }

        public readonly Stability? Stability;
        public readonly int? ReleaseNumber;
    }
}