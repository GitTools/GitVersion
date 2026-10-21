namespace GitVersion.Configuration;

// Keep the configuration's closed model graph visible to the trimmer, including
// the inherited branch properties on the root model. Arbitrary runtime types do
// not carry a guarantee that their properties survive NativeAOT compilation.
internal static class ConfigurationModelMetadata
{
    public static PropertyInfo[] GetProperties(Type type)
    {
        if (type == typeof(GitVersionConfiguration))
        {
            return typeof(GitVersionConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }
        if (type == typeof(BranchConfiguration))
        {
            return typeof(BranchConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }
        if (type == typeof(PreventIncrementConfiguration))
        {
            return typeof(PreventIncrementConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }
        if (type == typeof(IgnoreConfiguration))
        {
            return typeof(IgnoreConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }

        throw new InvalidOperationException($"Configuration model metadata is unavailable for '{type}'.");
    }
}
