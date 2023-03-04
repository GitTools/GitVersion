using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Mono.Cecil;

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

        string? version = string.Empty;
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

        string? version = string.Empty;
        this.versionWriter.WriteTo(asm, v => version = v);
        Assert.Multiple(() =>
        {
            Assert.That(asm, Is.Not.Null);
            Assert.That(version, Is.EqualTo("1.0.0-beta4"));
        });
    }

    private static Assembly GenerateAssembly(Version fileVersion, string prereleaseInfo)
    {
        var definition = new AssemblyNameDefinition("test-asm", fileVersion);

        var asmDef = AssemblyDefinition.CreateAssembly(definition, "test-asm", ModuleKind.Dll);
        var constructor = typeof(AssemblyInformationalVersionAttribute).GetConstructor(new[] { typeof(string) });
        var methodReference = asmDef.MainModule.ImportReference(constructor);
        var customAttribute = new CustomAttribute(methodReference);
        customAttribute.ConstructorArguments.Add(new CustomAttributeArgument(asmDef.MainModule.TypeSystem.String, fileVersion + prereleaseInfo));
        asmDef.CustomAttributes.Add(customAttribute);

        using var memoryStream = new MemoryStream();
        asmDef.Write(memoryStream);

        return Assembly.Load(memoryStream.ToArray());
    }
}
