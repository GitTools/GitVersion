namespace GitVersion.VersionAssemblyInfoResources
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using GitVersionCore.Extensions;

    public class AssemblyVersionInfoTemplates
    {
        static readonly IDictionary<string, FileInfo> assemblyInfoSourceList;

        static AssemblyVersionInfoTemplates()
        {
            assemblyInfoSourceList = GetEmbeddedVersionAssemblyFiles().ToDictionary(k => k.Extension, v => v);
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
