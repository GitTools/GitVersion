using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.App.Tests;

[TestFixture]
public class VersionWriterTests : TestBase
{
    private readonly IVersionWriter versionWriter;

    public VersionWriterTests()
    {
        var sp = ConfigureServices(services => services.AddModule(new GitVersionAppModule()));

        this.versionWriter = sp.GetRequiredService<IVersionWriter>();
    }
    [Test]
    public void WriteVersionShouldWriteFileVersionWithNoPrereleaseTag()
    {
        var asm = GenerateAssembly(new Version(1, 0, 0), "");

        var version = string.Empty;
        this.versionWriter.WriteTo(asm, v => version = v);
        Assert.Multiple(() =>
        {
            Assert.That(asm, Is.Not.Null);
            Assert.That(version, Is.EqualTo("1.0.0"));
        });
    }

    [Test]
    public void WriteVersionShouldWriteFileVersionWithPrereleaseTag()
    {
        var asm = GenerateAssembly(new Version(1, 0, 0), "-beta4");

        var version = string.Empty;
        this.versionWriter.WriteTo(asm, v => version = v);
        Assert.Multiple(() =>
        {
            Assert.That(asm, Is.Not.Null);
            Assert.That(version, Is.EqualTo("1.0.0-beta4"));
        });
    }

    private static Assembly GenerateAssembly(Version fileVersion, string prereleaseInfo)
    {
        var attribute = typeof(AssemblyInformationalVersionAttribute);
        var csharpCode = $@"[assembly: {attribute.FullName}(""{fileVersion + prereleaseInfo}"")]";
        var compilation = CSharpCompilation.Create("test-asm")
            .WithOptions(new(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(attribute.Assembly.Location))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(csharpCode));

        using var memoryStream = new MemoryStream();
        compilation.Emit(memoryStream);

        return Assembly.Load(memoryStream.ToArray());
    }
}
