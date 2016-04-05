namespace GitVersion.VersionAssemblyInfoResources
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using GitVersionCore.Extensions;

    public class AssemblyVersionInfoTemplates
    {
        static IDictionary<string, FileInfo> assemblyInfoSourceList;

        static AssemblyVersionInfoTemplates()
        {
            var enclosingNamespace = typeof(AssemblyVersionInfoTemplates).Namespace;

            var files = typeof(AssemblyVersionInfoTemplates)
                .Assembly
                .GetManifestResourceNames()
                .Where(n => n.StartsWith(enclosingNamespace ?? string.Empty)).Select(f => new FileInfo(f));

            assemblyInfoSourceList = files.ToDictionary(k => k.Extension, v => v);
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
    }
}
