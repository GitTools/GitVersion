namespace GitVersion.VersionCalculation
{
    public interface IVersionFilter
    {
        bool Exclude(BaseVersion version, out string reason);
    }
}
