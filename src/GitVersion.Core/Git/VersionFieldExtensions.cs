namespace GitVersion;

internal static class VersionFieldExtensions
{
    public static VersionField Consolidate(this VersionField source, VersionField? item, params VersionField?[] items)
    {
        VersionField result = source;
        foreach (VersionField? increment in new[] { item }.Concat(items))
        {
            if (result < increment) result = increment.Value;
        }
        return result;
    }
}
