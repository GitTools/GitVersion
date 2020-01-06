using System;
using System.IO;
using System.Reflection;
using GitVersion;
using Mono.Cecil;
using NUnit.Framework;

namespace GitVersionExe.Tests
{
    [TestFixture]
    public class VersionWriterTests
    {
        private readonly IVersionWriter versionWriter;

        public VersionWriterTests()
        {
            this.versionWriter = new VersionWriter();
        }
        [Test]
        public void WriteVersionShouldWriteFileVersionWithNoPrereleaseTag()
        {
            var asm = GenerateAssembly(new Version(1, 0, 0), "");

            string version = null;
            versionWriter.WriteTo(asm, v => version = v);

            Assert.IsNotNull(asm);
            Assert.AreEqual("1.0.0", version);
        }

        [Test]
        public void WriteVersionShouldWriteFileVersionWithPrereleaseTag()
        {
            var asm = GenerateAssembly(new Version(1, 0, 0), "-beta0004");

            string version = null;
            versionWriter.WriteTo(asm, v => version = v);

            Assert.IsNotNull(asm);
            Assert.AreEqual("1.0.0-beta0004", version);
        }

        private Assembly GenerateAssembly(Version fileVersion, string prereleaseInfo)
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
}
