namespace GitVersion.VersionAssemblyInfoResources
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using GitVersionCore.Extensions;
    using JetBrains.Annotations;

    public class AssemblyVersionInfoTemplates
    {
        static readonly IDictionary<string, FileInfo> assemblyInfoSourceList;
        static readonly IDictionary<string, string> assemblyInfoAddFormats;

        static AssemblyVersionInfoTemplates()
        {
            assemblyInfoSourceList = GetEmbeddedVersionAssemblyFiles().ToDictionary(k => k.Extension, v => v);
            // TODO: It would be nice to do something a bit more clever here, like reusing the VersionAssemblyInfo.* templates somehow. @asbjornu
            assemblyInfoAddFormats = new Dictionary<string, string>
            {
                {".cs", "[assembly: {0}]"},
                {".vb", "<assembly: {0}>"},
                {".fs", "[<assembly: {0}>]"}
            };
        }

        public static string GetAssemblyInfoTemplateFor(string assemblyInfoFile)
        {
            var fi = new FileInfo(assemblyInfoFile);
            if (assemblyInfoSourceList.ContainsKey(fi.Extension))
            {
                var template = assemblyInfoSourceList[fi.Extension];
                if (template != null)
                {
                    return template.Name.ReadAsStringFromEmbeddedResource<AssemblyVersionInfoTemplates>();
                }
            }
            return null;
        }

        public static string GetAssemblyInfoAddFormatFor([NotNull] string fileExtension)
        {
            if (fileExtension == null)
                throw new ArgumentNullException("fileExtension");

            string assemblyInfoAddFormat;
            if (!assemblyInfoAddFormats.TryGetValue(fileExtension, out assemblyInfoAddFormat))
                throw new NotSupportedException(string.Format("Unknown file extension '{0}'.", fileExtension));

            return assemblyInfoAddFormat;
        }

        public static bool IsSupported([NotNull] string fileExtension)
        {
            if (fileExtension == null)
                throw new ArgumentNullException("fileExtension");

            return assemblyInfoAddFormats.Keys.Contains(fileExtension, StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<FileInfo> GetEmbeddedVersionAssemblyFiles()
        {
            var enclosingNamespace = typeof(AssemblyVersionInfoTemplates).Namespace;

            if (enclosingNamespace == null)
                throw new InvalidOperationException("The AssemblyVersionInfoTemplates class is missing its namespace.");

            foreach (var name in typeof(AssemblyVersionInfoTemplates).Assembly.GetManifestResourceNames())
            {
                if (name.StartsWith(enclosingNamespace))
                    yield return new FileInfo(name);
            }
        }
    }
}