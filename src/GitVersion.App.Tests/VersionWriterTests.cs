using GitVersion.Core.Tests.Helpers;
using GitVersion.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Mono.Cecil;
using NUnit.Framework;

namespace GitVersion.App.Tests;

[TestFixture]
public class VersionWriterTests : TestBase
{
    private readonly IVersionWriter versionWriter;

    public VersionWriterTests()
    {
        var sp = ConfigureServices(services => services.AddModule(new GitVersionAppModule()));

        this.versionWriter = sp.GetService<IVersionWriter>();
    }
    [Test]
    public void WriteVersionShouldWriteFileVersionWithNoPrereleaseTag()
    {
        var asm = GenerateAssembly(new Version(1, 0, 0), "");

        string version = null;
        this.versionWriter.WriteTo(asm, v => version = v);

        Assert.IsNotNull(asm);
        Assert.AreEqual("1.0.0", version);
    }

    [Test]
    public void WriteVersionShouldWriteFileVersionWithPrereleaseTag()
    {
        var asm = GenerateAssembly(new Version(1, 0, 0), "-beta0004");

        string version = null;
        this.versionWriter.WriteTo(asm, v => version = v);

        Assert.IsNotNull(asm);
        Assert.AreEqual("1.0.0-beta0004", version);
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
