using System.Xml.Linq;
using GitVersion.Configuration;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using GitVersion.Logging;
using GitVersion.Output.AssemblyInfo;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Core.Tests;

[TestFixture]
[Parallelizable(ParallelScope.None)]
public class ProjectFileUpdaterTests : TestBase
{
    private const string TargetFramework = "net8.0";
    private IVariableProvider variableProvider;
    private ILog log;
    private IFileSystem fileSystem;
    private IProjectFileUpdater projectFileUpdater;
    private List<string> logMessages;

    [SetUp]
    public void Setup()
    {
        ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestCaseAttribute>();
        var sp = ConfigureServices();

        this.logMessages = [];
        this.log = new Log(new TestLogAppender(this.logMessages.Add));

        this.fileSystem = sp.GetRequiredService<IFileSystem>();
        this.variableProvider = sp.GetRequiredService<IVariableProvider>();
        this.projectFileUpdater = new ProjectFileUpdater(this.log, this.fileSystem);
    }

    [TearDown]
    public void Teardown() => this.projectFileUpdater.Dispose();

    [TestCase("Microsoft.NET.Sdk")]
    [TestCase("Microsoft.NET.Sdk.Worker")]
    [TestCase("Microsoft.NET.Sdk.Web")]
    [TestCase("Microsoft.NET.Sdk.WindowsDesktop")]
    [TestCase("Microsoft.NET.Sdk.Razor")]
    [TestCase("Microsoft.NET.Sdk.BlazorWebAssembly")]
    public void CanUpdateProjectFileWithSdkProjectFileXml(string sdk)
    {
        var xml = $"""
                   <Project Sdk="{sdk}">
                     <PropertyGroup>
                       <OutputType>Exe</OutputType>
                       <TargetFramework>{TargetFramework}</TargetFramework>
                     </PropertyGroup>
                   </Project>
                   """;
        var canUpdate = projectFileUpdater.CanUpdateProjectFile(XElement.Parse(xml));

        canUpdate.ShouldBe(true);
        logMessages.ShouldBeEmpty();
    }

    [TestCase($"""
               <Project Sdk="SomeOtherProject.Sdk">
                 <PropertyGroup>
                   <OutputType>Exe</OutputType>
                   <TargetFramework>{TargetFramework}</TargetFramework>
                 </PropertyGroup>
               </Project>
               """)]
    public void CannotUpdateProjectFileWithIncorrectProjectSdk(string xml)
    {
        var canUpdate = projectFileUpdater.CanUpdateProjectFile(XElement.Parse(xml));

        canUpdate.ShouldBe(false);

        logMessages.ShouldNotBeEmpty();
        logMessages.Count.ShouldBe(1);
        logMessages[0].ShouldContain("Specified project file Sdk (SomeOtherProject.Sdk) is not supported, please ensure the project sdk starts with 'Microsoft.NET.Sdk'");
    }

    [TestCase($"""
               <Project>
                 <PropertyGroup>
                   <OutputType>Exe</OutputType>
                   <TargetFramework>{TargetFramework}</TargetFramework>
                 </PropertyGroup>
               </Project>
               """)]
    public void CannotUpdateProjectFileWithMissingProjectSdk(string xml)
    {
        var canUpdate = projectFileUpdater.CanUpdateProjectFile(XElement.Parse(xml));

        canUpdate.ShouldBe(false);

        logMessages.ShouldNotBeEmpty();
        logMessages.Count.ShouldBe(1);
        logMessages[0].ShouldContain("Specified project file Sdk () is not supported, please ensure the project sdk starts with 'Microsoft.NET.Sdk'");
    }

    [TestCase($"""
               <Project Sdk="Microsoft.NET.Sdk">
                 <PropertyGroup>
                   <OutputType>Exe</OutputType>
                   <TargetFramework>{TargetFramework}</TargetFramework>
                   <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
                 </PropertyGroup>
               </Project>
               """)]
    public void CannotUpdateProjectFileWithoutAssemblyInfoGeneration(string xml)
    {
        var canUpdate = projectFileUpdater.CanUpdateProjectFile(XElement.Parse(xml));

        canUpdate.ShouldBe(false);

        logMessages.ShouldNotBeEmpty();
        logMessages.Count.ShouldBe(1);
        logMessages[0].ShouldContain("Project file specifies <GenerateAssemblyInfo>false</GenerateAssemblyInfo>: versions set in this project file will not affect the output artifacts");
    }

    [TestCase("""
              <Project Sdk="Microsoft.NET.Sdk">
              </Project>
              """)]
    public void CannotUpdateProjectFileWithoutAPropertyGroup(string xml)
    {
        var canUpdate = projectFileUpdater.CanUpdateProjectFile(XElement.Parse(xml));

        canUpdate.ShouldBe(false);

        logMessages.ShouldNotBeEmpty();
        logMessages.Count.ShouldBe(1);
        logMessages[0].ShouldContain("Unable to locate any <PropertyGroup> elements in specified project file. Are you sure it is in a correct format?");
    }

    [TestCase($"""
               <Project Sdk="Microsoft.NET.Sdk">
                 <PropertyGroup>
                   <OutputType>Exe</OutputType>
                   <TargetFramework>{TargetFramework}</TargetFramework>
                 </PropertyGroup>
               </Project>
               """
    )]
    public void UpdateProjectXmlVersionElementWithStandardXmlInsertsElement(string xml)
    {
        var variables = this.variableProvider.GetVariablesFor(
            SemanticVersion.Parse("2.0.0", RegexPatterns.Configuration.DefaultTagPrefixPattern), EmptyConfigurationBuilder.New.Build(), 0
        );
        var xmlRoot = XElement.Parse(xml);
        variables.AssemblySemVer.ShouldNotBeNull();
        ProjectFileUpdater.UpdateProjectVersionElement(xmlRoot, ProjectFileUpdater.AssemblyVersionElement, variables.AssemblySemVer);

        const string projectFileContent = $"""
                                           <Project Sdk="Microsoft.NET.Sdk">
                                             <PropertyGroup>
                                               <OutputType>Exe</OutputType>
                                               <TargetFramework>{TargetFramework}</TargetFramework>
                                               <AssemblyVersion>2.0.0.0</AssemblyVersion>
                                             </PropertyGroup>
                                           </Project>
                                           """;
        var expectedXml = XElement.Parse(projectFileContent);
        xmlRoot.ToString().ShouldBe(expectedXml.ToString());
    }

    [TestCase($"""
               <Project Sdk="Microsoft.NET.Sdk">
                 <PropertyGroup>
                   <OutputType>Exe</OutputType>
                   <TargetFramework>{TargetFramework}</TargetFramework>
                   <AssemblyVersion>1.0.0.0</AssemblyVersion>
                 </PropertyGroup>
               </Project>
               """
    )]
    public void UpdateProjectXmlVersionElementWithStandardXmlModifiesElement(string xml)
    {
        var variables = this.variableProvider.GetVariablesFor(SemanticVersion.Parse("2.0.0", RegexPatterns.Configuration.DefaultTagPrefixPattern), EmptyConfigurationBuilder.New.Build(), 0);
        var xmlRoot = XElement.Parse(xml);
        variables.AssemblySemVer.ShouldNotBeNull();
        ProjectFileUpdater.UpdateProjectVersionElement(xmlRoot, ProjectFileUpdater.AssemblyVersionElement, variables.AssemblySemVer);

        var projectFileContent = $"""
                                  <Project Sdk="Microsoft.NET.Sdk">
                                    <PropertyGroup>
                                      <OutputType>Exe</OutputType>
                                      <TargetFramework>{TargetFramework}</TargetFramework>
                                      <AssemblyVersion>2.0.0.0</AssemblyVersion>
                                    </PropertyGroup>
                                  </Project>
                                  """;
        var expectedXml = XElement.Parse(projectFileContent);
        xmlRoot.ToString().ShouldBe(expectedXml.ToString());
    }

    [TestCase($"""
               <Project Sdk="Microsoft.NET.Sdk">
                 <PropertyGroup>
                   <OutputType>Exe</OutputType>
                   <TargetFramework>{TargetFramework}</TargetFramework>
                   <AssemblyVersion>1.0.0.0</AssemblyVersion>
                 </PropertyGroup>
                 <PropertyGroup>
                   <AssemblyVersion>1.0.0.0</AssemblyVersion>
                 </PropertyGroup>
               </Project>
               """
    )]
    public void UpdateProjectXmlVersionElementWithDuplicatePropertyGroupsModifiesLastElement(string xml)
    {
        var variables = this.variableProvider.GetVariablesFor(SemanticVersion.Parse("2.0.0", RegexPatterns.Configuration.DefaultTagPrefixPattern), EmptyConfigurationBuilder.New.Build(), 0);
        var xmlRoot = XElement.Parse(xml);
        variables.AssemblySemVer.ShouldNotBeNull();
        ProjectFileUpdater.UpdateProjectVersionElement(xmlRoot, ProjectFileUpdater.AssemblyVersionElement, variables.AssemblySemVer);

        var projectFileContent = $"""
                                  <Project Sdk="Microsoft.NET.Sdk">
                                    <PropertyGroup>
                                      <OutputType>Exe</OutputType>
                                      <TargetFramework>{TargetFramework}</TargetFramework>
                                      <AssemblyVersion>1.0.0.0</AssemblyVersion>
                                    </PropertyGroup>
                                    <PropertyGroup>
                                      <AssemblyVersion>2.0.0.0</AssemblyVersion>
                                    </PropertyGroup>
                                  </Project>
                                  """;
        var expectedXml = XElement.Parse(projectFileContent);
        xmlRoot.ToString().ShouldBe(expectedXml.ToString());
    }

    [TestCase($"""
               <Project Sdk="Microsoft.NET.Sdk">
                 <PropertyGroup>
                   <OutputType>Exe</OutputType>
                   <TargetFramework>{TargetFramework}</TargetFramework>
                   <AssemblyVersion>1.0.0.0</AssemblyVersion>
                   <AssemblyVersion>1.0.0.0</AssemblyVersion>
                 </PropertyGroup>
               </Project>
               """
    )]
    public void UpdateProjectXmlVersionElementWithMultipleVersionElementsLastOneIsModified(string xml)
    {
        var variables = this.variableProvider.GetVariablesFor(SemanticVersion.Parse("2.0.0", RegexPatterns.Configuration.DefaultTagPrefixPattern), EmptyConfigurationBuilder.New.Build(), 0);
        var xmlRoot = XElement.Parse(xml);
        variables.AssemblySemVer.ShouldNotBeNull();
        ProjectFileUpdater.UpdateProjectVersionElement(xmlRoot, ProjectFileUpdater.AssemblyVersionElement, variables.AssemblySemVer);

        var projectFileContent = $"""
                                  <Project Sdk="Microsoft.NET.Sdk">
                                    <PropertyGroup>
                                      <OutputType>Exe</OutputType>
                                      <TargetFramework>{TargetFramework}</TargetFramework>
                                      <AssemblyVersion>1.0.0.0</AssemblyVersion>
                                      <AssemblyVersion>2.0.0.0</AssemblyVersion>
                                    </PropertyGroup>
                                  </Project>
                                  """;
        var expectedXml = XElement.Parse(projectFileContent);
        xmlRoot.ToString().ShouldBe(expectedXml.ToString());
    }

    [TestCase("""

              <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                  <OutputType>Exe</OutputType>
                  <TargetFramework>net8.0</TargetFramework>
                </PropertyGroup>
              </Project>
              """)]
    public void UpdateProjectFileAddsVersionToFile(string xml)
    {
        var workingDirectory = PathHelper.GetTempPath();
        var fileName = PathHelper.Combine(workingDirectory, "TestProject.csproj");

        VerifyAssemblyInfoFile(xml, fileName, AssemblyVersioningScheme.MajorMinorPatch, (fs, variables) =>
        {
            using var projFileUpdater = new ProjectFileUpdater(this.log, fs);
            projFileUpdater.Execute(variables, new(workingDirectory, false, fileName));

            const string expectedXml = $"""
                                        <Project Sdk="Microsoft.NET.Sdk">
                                          <PropertyGroup>
                                            <OutputType>Exe</OutputType>
                                            <TargetFramework>{TargetFramework}</TargetFramework>
                                            <AssemblyVersion>2.3.1.0</AssemblyVersion>
                                            <FileVersion>2.3.1.0</FileVersion>
                                            <InformationalVersion>2.3.1+3.Branch.foo.Sha.hash</InformationalVersion>
                                            <Version>2.3.1</Version>
                                          </PropertyGroup>
                                        </Project>
                                        """;
            var transformedXml = fs.ReadAllText(fileName);
            transformedXml.ShouldBe(XElement.Parse(expectedXml).ToString());
        });
    }

    private void VerifyAssemblyInfoFile(
        string projectFileContent,
        string fileName,
        AssemblyVersioningScheme versioningScheme = AssemblyVersioningScheme.MajorMinorPatch,
        Action<IFileSystem, GitVersionVariables>? verify = null)
    {
        this.fileSystem = Substitute.For<IFileSystem>();
        var version = new SemanticVersion { BuildMetaData = new("versionSourceHash", 3, "foo", "hash", "shortHash", DateTimeOffset.Now, 0), Major = 2, Minor = 3, Patch = 1 };

        this.fileSystem.Exists(fileName).Returns(true);
        this.fileSystem.ReadAllText(fileName).Returns(projectFileContent);
        this.fileSystem.When(f => f.WriteAllText(fileName, Arg.Any<string>())).Do(c =>
        {
            projectFileContent = c.ArgAt<string>(1);
            this.fileSystem.ReadAllText(fileName).Returns(projectFileContent);
        });

        var configuration = EmptyConfigurationBuilder.New.WithAssemblyVersioningScheme(versioningScheme).Build();
        var variables = this.variableProvider.GetVariablesFor(version, configuration, 0);

        verify?.Invoke(this.fileSystem, variables);
    }
}
