using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Xml.Linq;
using Path = System.IO.Path;

namespace Docs.Utilities;

public static class DocsApi
{
    // Published binaries define the API. Tagged source supplies comments missing from the packages.
    public static void Prepare(string source, string assemblies)
    {
        var references = Directory.GetFiles(assemblies, "*.dll")
            .Select(p => MetadataReference.CreateFromFile(p)).ToArray();
        var inventory = new List<string>();
        var routes = new List<string>();
        foreach (var assembly in references.Where(r => Path.GetFileName(r.FilePath!).StartsWith("GitVersion", StringComparison.OrdinalIgnoreCase)))
        {
            var name = Path.GetFileNameWithoutExtension(assembly.FilePath!);
            var metadata = CSharpCompilation.Create("Inventory", references: references);
            var symbol = metadata.GetAssemblyOrModuleSymbol(assembly) as IAssemblySymbol
                ?? throw new InvalidOperationException($"Could not inspect {name}.");
            var exported = Types(symbol.GlobalNamespace).Where(Public).ToArray();
            inventory.AddRange(exported.Select(t => t.GetDocumentationCommentId()!).Where(id => id is not null));
            routes.AddRange(exported.Select(t => t.ContainingNamespace.ToDisplayString() + "/" + t.MetadataName.Replace('`', '_') + "/index.html"));
            var project = Path.Combine(source, "src", name == "gitversion" ? "GitVersion.App" : name);
            if (!Directory.Exists(project)) continue;
            var trees = Directory.GetFiles(project, "*.cs", SearchOption.AllDirectories)
                .Where(p => !Path.GetRelativePath(project, p).Split(Path.DirectorySeparatorChar).Any(s => s is "bin" or "obj" or "Templates"))
                .Select(p => CSharpSyntaxTree.ParseText(File.ReadAllText(p), new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose), p)).ToArray();
            var compilation = CSharpCompilation.Create(name, trees, references.Where(r => r != assembly),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var path = Path.Combine(assemblies, name + ".xml");
            var document = File.Exists(path) ? XDocument.Load(path) : new XDocument(new XElement("doc", new XElement("assembly", new XElement("name", name)), new XElement("members")));
            var members = document.Root!.Element("members")!;
            var existing = members.Elements("member").Select(m => (string?)m.Attribute("name")).ToHashSet();
            foreach (var tree in trees)
            {
                var model = compilation.GetSemanticModel(tree);
                foreach (var node in tree.GetRoot().DescendantNodes().Where(n => n is MemberDeclarationSyntax or VariableDeclaratorSyntax))
                {
                    var declared = model.GetDeclaredSymbol(node);
                    var id = declared?.GetDocumentationCommentId();
                    if (id is null || existing.Contains(id)) continue;
                    var xml = declared!.GetDocumentationCommentXml();
                    if (string.IsNullOrWhiteSpace(xml)) continue;
                    try { members.Add(XElement.Parse(xml)); existing.Add(id); }
                    catch (System.Xml.XmlException e) { throw new InvalidOperationException($"Invalid API comment for {id} in {tree.FilePath}", e); }
                }
            }
            document.Save(path);
        }
        File.WriteAllLines(Path.Combine(assemblies, "public-types.txt"), inventory.Order(StringComparer.Ordinal));
        File.WriteAllLines(Path.Combine(assemblies, "public-type-routes.txt"), routes.Order(StringComparer.Ordinal));
    }

    private static IEnumerable<INamedTypeSymbol> Types(INamespaceOrTypeSymbol parent)
    {
        foreach (var member in parent.GetMembers())
        {
            if (member is INamedTypeSymbol type) yield return type;
            if (member is INamespaceOrTypeSymbol container)
                foreach (var child in Types(container)) yield return child;
        }
    }

    private static bool Public(INamedTypeSymbol type) => type.DeclaredAccessibility == Accessibility.Public
        && !type.Name.Contains('<') && type.ContainingNamespace.ToDisplayString().StartsWith("GitVersion", StringComparison.Ordinal)
        && (type.ContainingType is null || Public(type.ContainingType));
}
