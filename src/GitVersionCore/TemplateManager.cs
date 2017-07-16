namespace GitVersion
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using GitVersionCore.Extensions;

    enum TemplateType
    {
        VersionAssemblyInfoResources,
        GitVersionInformationResources
    }

    class TemplateManager
    {
        readonly Dictionary<string, string> templates;
        readonly Dictionary<string, string> addFormats;

        public TemplateManager(TemplateType templateType)
        {
            templates = GetEmbeddedTemplates(templateType, "Templates").ToDictionary(k => Path.GetExtension(k), v => v);
            addFormats = GetEmbeddedTemplates(templateType, "AddFormats").ToDictionary(k => Path.GetExtension(k), v => v);
        }

        public string GetTemplateFor(string fileExtension)
        {
            string result = null;
            string template;

            if (templates.TryGetValue(fileExtension, out template) && template != null)
            {
                result =  template.ReadAsStringFromEmbeddedResource<TemplateManager>();
            }

            return result;
        }

        public string GetAddFormatFor(string fileExtension)
        {
            string result = null;
            string addFormat;

            if(addFormats.TryGetValue(fileExtension, out addFormat) && addFormat != null)
            {
                result = addFormat.ReadAsStringFromEmbeddedResource<TemplateManager>();
            }

            return result;
        }

        public bool IsSupported(string fileExtension)
        {
            if (fileExtension == null)
            {
                throw new ArgumentNullException(nameof(fileExtension));
            }

            return addFormats.Keys.Contains(fileExtension, StringComparer.OrdinalIgnoreCase);
        }

        static IEnumerable<string> GetEmbeddedTemplates(TemplateType templateType, string templateCategory)
        {
            foreach (var name in typeof(TemplateManager).Assembly.GetManifestResourceNames())
            {
                if (name.Contains(templateType.ToString()) && name.Contains(templateCategory))
                {
                    yield return name;
                }
            }
        }
    }
}
