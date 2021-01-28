using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitVersion.Extensions;

namespace GitVersion.VersionConverters
{
    internal enum TemplateType
    {
        AssemblyInfo,
        GitVersionInfo
    }

    internal class TemplateManager
    {
        private readonly Dictionary<string, string> templates;
        private readonly Dictionary<string, string> addFormats;

        public TemplateManager(TemplateType templateType)
        {
            templates = GetEmbeddedTemplates(templateType, "Templates").ToDictionary(Path.GetExtension, v => v, StringComparer.OrdinalIgnoreCase);
            addFormats = GetEmbeddedTemplates(templateType, "AddFormats").ToDictionary(Path.GetExtension, v => v, StringComparer.OrdinalIgnoreCase);
        }

        public string GetTemplateFor(string fileExtension)
        {
            if (fileExtension == null)
            {
                throw new ArgumentNullException(nameof(fileExtension));
            }

            string result = null;

            if (templates.TryGetValue(fileExtension, out var template) && template != null)
            {
                result = template.ReadAsStringFromEmbeddedResource<TemplateManager>();
            }

            return result;
        }

        public string GetAddFormatFor(string fileExtension)
        {
            if (fileExtension == null)
            {
                throw new ArgumentNullException(nameof(fileExtension));
            }

            string result = null;

            if (addFormats.TryGetValue(fileExtension, out var addFormat) && addFormat != null)
            {
                result = addFormat.ReadAsStringFromEmbeddedResource<TemplateManager>().TrimEnd('\r', '\n');
            }

            return result;
        }

        public bool IsSupported(string fileExtension)
        {
            if (fileExtension == null)
            {
                throw new ArgumentNullException(nameof(fileExtension));
            }

            return templates.ContainsKey(fileExtension);
        }

        private static IEnumerable<string> GetEmbeddedTemplates(TemplateType templateType, string templateCategory)
        {

            var assy = typeof(TemplateManager).Assembly;

            foreach (var name in assy.GetManifestResourceNames())
            {
                if (name.Contains(templateType.ToString()) && name.Contains(templateCategory))
                {
                    yield return name;
                }
            }
        }
    }
}
