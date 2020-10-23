using GitVersion;
using GitVersion.Extensions;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;
using GitVersion.VersionConverters.AssemblyInfo;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;
using System.Xml.Linq;

namespace GitVersionCore.Tests
{
    [TestFixture]
    [Parallelizable(ParallelScope.None)]
    public class ProjectFileUpdaterTests : TestBase
    {
        private IVariableProvider variableProvider;
        private ILog log;
        private IFileSystem fileSystem;

        [SetUp]
        public void Setup()
        {
            ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestCaseAttribute>();
            var sp = ConfigureServices();
            log = Substitute.For<ILog>();
            fileSystem = sp.GetService<IFileSystem>();
            variableProvider = sp.GetService<IVariableProvider>();
        }

        [TestCase(@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
  </PropertyGroup>
</Project>
")]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
        public void CanUpdateProjectFileWithStandardProjectFileXml(string xml)
        {
            using var projectFileUpdater = new ProjectFileUpdater(log, fileSystem);

            var canUpdate = projectFileUpdater.CanUpdateProjectFile(XElement.Parse(xml));

            canUpdate.ShouldBe(true);
        }

        [TestCase(@"
<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
  </PropertyGroup>
</Project>
")]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
        public void CanUpdateProjectFileWithStandardWebProjectFileXml(string xml)
        {
            using var projectFileUpdater = new ProjectFileUpdater(log, fileSystem);

            var canUpdate = projectFileUpdater.CanUpdateProjectFile(XElement.Parse(xml));

            canUpdate.ShouldBe(true);
        }

        [TestCase(@"
<Project Sdk=""Microsoft.NET.Sdk.WindowsDesktop"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net461</TargetFramework>
  </PropertyGroup>
</Project>
")]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
        public void CanUpdateProjectFileWithStandardDesktopProjectFileXml(string xml)
        {
            using var projectFileUpdater = new ProjectFileUpdater(log, fileSystem);

            var canUpdate = projectFileUpdater.CanUpdateProjectFile(XElement.Parse(xml));

            canUpdate.ShouldBe(true);
        }

        [TestCase(@"
<Project Sdk=""SomeOtherProject.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
  </PropertyGroup>
</Project>
")]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
        public void CannotUpdateProjectFileWithIncorrectProjectSdk(string xml)
        {
            using var projectFileUpdater = new ProjectFileUpdater(log, fileSystem);

            var canUpdate = projectFileUpdater.CanUpdateProjectFile(XElement.Parse(xml));

            canUpdate.ShouldBe(false);
        }

        [TestCase(@"
<Project>
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
  </PropertyGroup>
</Project>
")]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
        public void CannotUpdateProjectFileWithMissingProjectSdk(string xml)
        {
            using var projectFileUpdater = new ProjectFileUpdater(log, fileSystem);

            var canUpdate = projectFileUpdater.CanUpdateProjectFile(XElement.Parse(xml));

            canUpdate.ShouldBe(false);
        }

        [TestCase(@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  </PropertyGroup>
</Project>
")]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
        public void CannotUpdateProjectFileWithoutAssemblyInfoGeneration(string xml)
        {
            using var projectFileUpdater = new ProjectFileUpdater(log, fileSystem);

            var canUpdate = projectFileUpdater.CanUpdateProjectFile(XElement.Parse(xml));

            canUpdate.ShouldBe(false);
        }

        [TestCase(@"
<Project Sdk=""Microsoft.NET.Sdk"">
</Project>
")]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
        public void CannotUpdateProjectFileWithoutAPropertyGroup(string xml)
        {
            using var projectFileUpdater = new ProjectFileUpdater(log, fileSystem);

            var canUpdate = projectFileUpdater.CanUpdateProjectFile(XElement.Parse(xml));

            canUpdate.ShouldBe(false);
        }

        [TestCase(@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
  </PropertyGroup>
</Project>"
        )]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
        public void UpdateProjectXmlVersionElementWithStandardXmlInsertsElement(string xml)
        {
            using var projectFileUpdater = new ProjectFileUpdater(log, fileSystem);

            var variables = variableProvider.GetVariablesFor(SemanticVersion.Parse("2.0.0", "v"), new TestEffectiveConfiguration(), false);
            var xmlRoot = XElement.Parse(xml);
            projectFileUpdater.UpdateProjectVersionElement(xmlRoot, ProjectFileUpdater.AssemblyVersionElement, variables.AssemblySemVer);

            var expectedXml = XElement.Parse(@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <AssemblyVersion>2.0.0.0</AssemblyVersion>
  </PropertyGroup>
</Project>");
            xmlRoot.ToString().ShouldBe(expectedXml.ToString());
        }

        [TestCase(@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
  </PropertyGroup>
</Project>"
        )]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
        public void UpdateProjectXmlVersionElementWithStandardXmlModifiesElement(string xml)
        {
            using var projectFileUpdater = new ProjectFileUpdater(log, fileSystem);

            var variables = variableProvider.GetVariablesFor(SemanticVersion.Parse("2.0.0", "v"), new TestEffectiveConfiguration(), false);
            var xmlRoot = XElement.Parse(xml);
            projectFileUpdater.UpdateProjectVersionElement(xmlRoot, ProjectFileUpdater.AssemblyVersionElement, variables.AssemblySemVer);

            var expectedXml = XElement.Parse(@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <AssemblyVersion>2.0.0.0</AssemblyVersion>
  </PropertyGroup>
</Project>");
            xmlRoot.ToString().ShouldBe(expectedXml.ToString());
        }

        [TestCase(@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
  </PropertyGroup>
  <PropertyGroup>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
  </PropertyGroup>
</Project>"
        )]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
        public void UpdateProjectXmlVersionElementWithDuplicatePropertyGroupsModifiesLastElement(string xml)
        {
            using var projectFileUpdater = new ProjectFileUpdater(log, fileSystem);

            var variables = variableProvider.GetVariablesFor(SemanticVersion.Parse("2.0.0", "v"), new TestEffectiveConfiguration(), false);
            var xmlRoot = XElement.Parse(xml);
            projectFileUpdater.UpdateProjectVersionElement(xmlRoot, ProjectFileUpdater.AssemblyVersionElement, variables.AssemblySemVer);

            var expectedXml = XElement.Parse(@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
  </PropertyGroup>
  <PropertyGroup>
    <AssemblyVersion>2.0.0.0</AssemblyVersion>
  </PropertyGroup>
</Project>");
            xmlRoot.ToString().ShouldBe(expectedXml.ToString());
        }

        [TestCase(@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
  </PropertyGroup>
</Project>"
        )]

        [Category(NoMono)]
        [Description(NoMonoDescription)]
        public void UpdateProjectXmlVersionElementWithMultipleVersionElementsLastOneIsModified(string xml)
        {
            using var projectFileUpdater = new ProjectFileUpdater(log, fileSystem);

            var variables = variableProvider.GetVariablesFor(SemanticVersion.Parse("2.0.0", "v"), new TestEffectiveConfiguration(), false);
            var xmlRoot = XElement.Parse(xml);
            projectFileUpdater.UpdateProjectVersionElement(xmlRoot, ProjectFileUpdater.AssemblyVersionElement, variables.AssemblySemVer);

            var expectedXml = XElement.Parse(@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
    <AssemblyVersion>2.0.0.0</AssemblyVersion>
  </PropertyGroup>
</Project>");
            xmlRoot.ToString().ShouldBe(expectedXml.ToString());
        }

        [TestCase(@"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
  </PropertyGroup>
</Project>")]
        [Category(NoMono)]
        [Description(NoMonoDescription)]
        public void UpdateProjectFileAddsVersionToFile(string xml)
        {
            var fileName = Path.Combine(Path.GetTempPath(), "TestProject.csproj");

            VerifyAssemblyInfoFile(xml, fileName, AssemblyVersioningScheme.MajorMinorPatch, verify: (fs, variables) =>
            {
                using var projectFileUpdater = new ProjectFileUpdater(log, fs);
                projectFileUpdater.Execute(variables, new AssemblyInfoContext(Path.GetTempPath(), false, fileName));

                var expectedXml = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp3.1</TargetFramework>
    <AssemblyVersion>2.3.1.0</AssemblyVersion>
    <FileVersion>2.3.1.0</FileVersion>
    <InformationalVersion>2.3.1+3.Branch.foo.Sha.hash</InformationalVersion>
    <Version>2.3.1</Version>
  </PropertyGroup>
</Project>";
                var transformedXml = fs.ReadAllText(fileName);
                transformedXml.ShouldBe(XElement.Parse(expectedXml).ToString());
            });
        }

        private void VerifyAssemblyInfoFile(
            string projectFileContent,
            string fileName,
            AssemblyVersioningScheme versioningScheme = AssemblyVersioningScheme.MajorMinorPatch,
            Action<IFileSystem, VersionVariables> verify = null)
        {
            fileSystem = Substitute.For<IFileSystem>();
            var version = new SemanticVersion
            {
                BuildMetaData = new SemanticVersionBuildMetaData("versionSourceHash", 3, "foo", "hash", "shortHash", DateTimeOffset.Now, 0),
                Major = 2,
                Minor = 3,
                Patch = 1
            };

            fileSystem.Exists(fileName).Returns(true);
            fileSystem.ReadAllText(fileName).Returns(projectFileContent);
            fileSystem.When(f => f.WriteAllText(fileName, Arg.Any<string>())).Do(c =>
            {
                projectFileContent = c.ArgAt<string>(1);
                fileSystem.ReadAllText(fileName).Returns(projectFileContent);
            });

            var config = new TestEffectiveConfiguration(assemblyVersioningScheme: versioningScheme);
            var variables = variableProvider.GetVariablesFor(version, config, false);

            verify?.Invoke(fileSystem, variables);
        }
    }
}
