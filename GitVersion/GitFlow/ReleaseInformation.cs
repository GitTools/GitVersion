namespace GitVersion
{
    public class ReleaseInformation
    {
        public ReleaseInformation(Stability? stability, int? releaseNumber)
        {
            ReleaseNumber = releaseNumber;
            Stability = stability;
        }

        public Stability? Stability;
        public int? ReleaseNumber;
    }
}