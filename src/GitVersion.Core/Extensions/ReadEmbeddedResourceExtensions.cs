namespace GitVersion.Extensions;

public static class ReadEmbeddedResourceExtensions
{
    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="resourceName">Should include Namespace separated path to resource in assembly referenced by <typeparamref name="T"/></param>
    /// <returns></returns>
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
