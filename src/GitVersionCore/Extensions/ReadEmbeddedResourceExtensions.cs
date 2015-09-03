namespace GitVersionCore.Extensions
{
    using System.IO;

    public static class ReadEmbeddedResourceExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resourceName">Should include Namespace separated path to resource in assembly referenced by <typeparamref name="T"/></param>
        /// <returns></returns>
        public static string ReadAsStringFromEmbeddedResource<T>(this string resourceName)
        {
            using (var stream = resourceName.ReadFromEmbeddedResource<T>())
            {
                using (var rdr = new StreamReader(stream))
                {
                    return rdr.ReadToEnd();
                }
            }
        }

        public static Stream ReadFromEmbeddedResource<T>(this string resourceName)
        {
            var assembly = typeof(T).Assembly;
            return assembly.GetManifestResourceStream(resourceName);
        }
    }
}
