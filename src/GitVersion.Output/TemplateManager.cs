using GitVersion.Extensions;
using GitVersion.Helpers;

namespace GitVersion.Output;

internal enum TemplateType
{
    AssemblyInfo,
    GitVersionInfo
}

internal class TemplateManager(TemplateType templateType)
{
    private readonly Dictionary<string, string> templates = GetEmbeddedTemplates(templateType, "Templates").ToDictionary(tuple => tuple.ext, tuple => tuple.name, StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> addFormats = GetEmbeddedTemplates(templateType, "AddFormats").ToDictionary(tuple => tuple.ext, tuple => tuple.name, StringComparer.OrdinalIgnoreCase);

    public string? GetTemplateFor(string? fileExtension)
    {
        ArgumentNullException.ThrowIfNull(fileExtension);

        string? result = null;

        if (this.templates.TryGetValue(fileExtension, out var template))
        {
            result = template.ReadAsStringFromEmbeddedResource<TemplateManager>();
        }

        return result;
    }

    public string? GetAddFormatFor(string? fileExtension)
    {
        ArgumentNullException.ThrowIfNull(fileExtension);

        string? result = null;

        if (this.addFormats.TryGetValue(fileExtension, out var addFormat))
        {
            result = addFormat.ReadAsStringFromEmbeddedResource<TemplateManager>().TrimEnd('\r', '\n');
        }

        return result;
    }

    public bool IsSupported(string fileExtension)
    {
        ArgumentNullException.ThrowIfNull(fileExtension);

        return this.templates.ContainsKey(fileExtension);
    }

    private static IEnumerable<(string ext, string name)> GetEmbeddedTemplates(TemplateType templateType, string templateCategory)
    {
        var assembly = typeof(TemplateManager).Assembly;

        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (!name.Contains(templateType.ToString()) || !name.Contains(templateCategory)) continue;
            var extension = FileSystemHelper.Path.GetExtension(name);
            if (string.IsNullOrWhiteSpace(extension))
            {
                continue;
            }
            yield return (ext: extension, name);
        }
    }
}
