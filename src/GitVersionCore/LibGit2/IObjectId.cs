namespace GitVersion
{
    public interface IObjectId
    {
        string Sha { get; }
        string ToString(int prefixLength);
    }
}
