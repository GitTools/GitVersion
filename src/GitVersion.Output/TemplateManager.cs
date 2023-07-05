using GitVersion.Extensions;

namespace GitVersion.Output;

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
        this.templates = GetEmbeddedTemplates(templateType, "Templates").ToDictionary(tuple => tuple.ext, tuple => tuple.name, StringComparer.OrdinalIgnoreCase);
        this.addFormats = GetEmbeddedTemplates(templateType, "AddFormats").ToDictionary(tuple => tuple.ext, tuple => tuple.name, StringComparer.OrdinalIgnoreCase);
    }

    public string? GetTemplateFor(string fileExtension)
    {
        if (fileExtension == null)
        {
            throw new ArgumentNullException(nameof(fileExtension));
        }

        string? result = null;

        if (this.templates.TryGetValue(fileExtension, out var template))
        {
            result = template.ReadAsStringFromEmbeddedResource<TemplateManager>();
        }

        return result;
    }

    public string? GetAddFormatFor(string fileExtension)
    {
        if (fileExtension == null)
        {
            throw new ArgumentNullException(nameof(fileExtension));
        }

        string? result = null;

        if (this.addFormats.TryGetValue(fileExtension, out var addFormat))
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

        return this.templates.ContainsKey(fileExtension);
    }

    private static IEnumerable<(string ext, string name)> GetEmbeddedTemplates(TemplateType templateType, string templateCategory)
    {
        var assembly = typeof(TemplateManager).Assembly;

        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (name.Contains(templateType.ToString()) && name.Contains(templateCategory))
            {
                yield return (ext: Path.GetExtension(name), name);
            }
        }
    }
}
