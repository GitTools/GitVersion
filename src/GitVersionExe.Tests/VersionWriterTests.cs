﻿namespace GitVersionExe.Tests
{
    using System;
    using System.IO;
    using System.Reflection;
    using GitVersion;
    using Mono.Cecil;
    using NUnit.Framework;

    [TestFixture]
    public class VersionWriterTests
    {
        [Test]
        public void WriteVersion_ShouldWriteFileVersion_WithNoPrereleaseTag()
        {
            var asm = GenerateAssembly(new Version(1, 0, 0), "");

            string version = null;
            VersionWriter.WriteTo(asm, v => version = v);

            Assert.IsNotNull(asm);
            Assert.AreEqual("1.0.0", version);
        }

        [Test]
        [Category("NoMono")]
        [Description("Seems assembly info is slightly different on mono, this test fails with no pre-release info.")]
        public void WriteVersion_ShouldWriteFileVersion_WithPrereleaseTag()
        {
            var asm = GenerateAssembly(new Version(1, 0, 0), "-beta0004");

            string version = null;
            VersionWriter.WriteTo(asm, v => version = v);

            Assert.IsNotNull(asm);
            Assert.AreEqual("1.0.0-beta0004", version);
        }

        private Assembly GenerateAssembly(Version fileVersion, string prereleaseInfo)
        {
            var definition = new AssemblyNameDefinition("test-asm", fileVersion);

            var asmDef = AssemblyDefinition.CreateAssembly(definition, "test-asm", ModuleKind.Dll);
            var constructor = typeof(AssemblyInformationalVersionAttribute).GetConstructor(new[] { typeof(string) });
            var methodReference = asmDef.MainModule.Import(constructor);
            var customAttribute = new CustomAttribute(methodReference);
            customAttribute.ConstructorArguments.Add(new CustomAttributeArgument(asmDef.MainModule.TypeSystem.String, fileVersion + prereleaseInfo));
            asmDef.CustomAttributes.Add(customAttribute);

            using (var memoryStream = new MemoryStream())
            {
                asmDef.Write(memoryStream);

                return Assembly.Load(memoryStream.ToArray());
            }
        }
    }
}
