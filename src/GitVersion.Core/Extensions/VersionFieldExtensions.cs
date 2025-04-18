namespace GitVersion.Extensions;

internal static class VersionFieldExtensions
{
    public static VersionField Consolidate(this VersionField source, VersionField? item, params VersionField?[] items)
    {
        var result = source;
        foreach (var increment in new[] { item }.Concat(items))
        {
            if (result < increment) result = increment.Value;
        }
        return result;
    }
}
