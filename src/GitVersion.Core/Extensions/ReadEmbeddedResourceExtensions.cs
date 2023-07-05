namespace GitVersion.Extensions;

public static class ReadEmbeddedResourceExtensions
{
    public static string ReadAsStringFromEmbeddedResource<T>(this string resourceName)
        => ReadAsStringFromEmbeddedResource(resourceName, typeof(T).Assembly);

    public static string ReadAsStringFromEmbeddedResource(this string resourceName, Assembly assembly)
    {
        using var stream = resourceName.ReadFromEmbeddedResource(assembly);
        using var streamReader = new StreamReader(stream);
        return streamReader.ReadToEnd();
    }

    private static Stream ReadFromEmbeddedResource(this string resourceName, Assembly assembly)
    {
        assembly.NotNull();

        return assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource {resourceName}");
    }
}
