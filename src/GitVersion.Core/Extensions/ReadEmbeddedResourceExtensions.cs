namespace GitVersion.Extensions;

/// <summary>Extension methods on <see cref="string"/> for reading embedded assembly resources.</summary>
public static class ReadEmbeddedResourceExtensions
{
    extension(string resourceName)
    {
        /// <summary>Reads the embedded resource identified by this name from the assembly that contains <typeparamref name="T"/> and returns its content as a string.</summary>
        public string ReadAsStringFromEmbeddedResource<T>()
            => ReadAsStringFromEmbeddedResource(resourceName, typeof(T).Assembly);

        /// <summary>Reads the embedded resource identified by this name from the given <paramref name="assembly"/> and returns its content as a string.</summary>
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
