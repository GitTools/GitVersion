namespace GitVersion.Extensions;

public static class ReadEmbeddedResourceExtensions
{
    extension(string resourceName)
    {
        public string ReadAsStringFromEmbeddedResource<T>()
            => ReadAsStringFromEmbeddedResource(resourceName, typeof(T).Assembly);

        public string ReadAsStringFromEmbeddedResource(Assembly assembly)
        {
            using var stream = resourceName.ReadFromEmbeddedResource(assembly);
            using var streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }

        private Stream ReadFromEmbeddedResource(Assembly assembly)
        {
            assembly.NotNull();

            return assembly.GetManifestResourceStream(resourceName)
                   ?? throw new InvalidOperationException($"Could not find embedded resource {resourceName}");
        }
    }
}
