namespace GitVersionExe.Tests
{
    using System;
    using System.CodeDom.Compiler;
    using System.Reflection;
    using GitVersion;
    using NUnit.Framework;

    [TestFixture]
    public class VersionWriterTests
    {
        [Category("NoMono")]
        [Description("Won't run on Mono because generating an assembly at runtime does not work on mono")]
        [Test]
        public void WriteVersion_ShouldWriteFileVersion_WithNoPrereleaseTag()
        {
            var asm = GenerateAssembly(new Version(1, 0, 0), "");

            string version = null;
            VersionWriter.WriteTo(asm, v => version = v);

            Assert.IsNotNull(asm);
            Assert.AreEqual("1.0.0", version);
        }

        [Category("NoMono")]
        [Description("Won't run on Mono because generating an assembly at runtime does not work on mono")]
        [Test]
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
            var source = string.Format(@"
using System.Reflection;
using System.Runtime.CompilerServices;
[assembly: AssemblyTitle(""GitVersion.DynamicUnitTests"")]
[assembly: AssemblyProduct(""GitVersion"")]
[assembly: AssemblyVersion(""{0}"")]
[assembly: AssemblyFileVersion(""{0}"")]
[assembly: AssemblyInformationalVersion(""{0}{1}"")]

public class B
{{
    public static int k=7;
}}
", fileVersion, prereleaseInfo);

            CompilerParameters parameters = new CompilerParameters
            {
                GenerateInMemory = true,
                GenerateExecutable = false,
                OutputAssembly = "GitVersion.DynamicUnitTests.dll"
            };

            var r = CodeDomProvider.CreateProvider("CSharp").CompileAssemblyFromSource(parameters, source);
            return r.CompiledAssembly;
        }
    }
}
