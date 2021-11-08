namespace Common.Addins.Cake.Coverlet;

internal static class BuilderExtension
{
    internal static ProcessArgumentBuilder AppendMSBuildProperty(this ProcessArgumentBuilder builder, string propertyName, string value)
    {
        builder.AppendSwitch($"/property:{propertyName}", "=", value);
        return builder;
    }

    internal static ProcessArgumentBuilder AppendMSBuildPropertyQuoted(this ProcessArgumentBuilder builder, string propertyName, string value)
    {
        builder.AppendSwitchQuoted($"/property:{propertyName}", "=", value);
        return builder;
    }

    internal static ProcessArgumentBuilder AppendPropertyList(this ProcessArgumentBuilder builder, string propertyName, IEnumerable<string> values)
    {
        builder.Append($"/property:{propertyName}=\\\"{string.Join(",", values.Select(s => s.Trim()))}\\\"");
        return builder;
    }

    internal static ProcessArgumentBuilder AppendSwitchQuoted(this ProcessArgumentBuilder builder, string @switch, IEnumerable<string> values)
    {
        foreach (var type in values.Select(s => s.Trim()))
        {
            builder.AppendSwitchQuoted(@switch, type);
        }
        return builder;
    }

    internal static ProcessArgumentBuilder AppendSwitch(this ProcessArgumentBuilder builder, string @switch, IEnumerable<string> values)
    {
        foreach (var type in values.Select(s => s.Trim()))
        {
            builder.AppendSwitch(@switch, type);
        }
        return builder;
    }
}
