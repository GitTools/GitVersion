using System.IO.Abstractions;
using System.Xml.Linq;
using GitVersion.Configuration;
using GitVersion.Core;
using GitVersion.Core.Tests.Helpers;
using GitVersion.Helpers;
using GitVersion.Output.AssemblyInfo;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;

namespace GitVersion.Output.Tests;

[TestFixture]
[Parallelizable(ParallelScope.None)]
public class ProjectFileUpdaterTests : TestBase
{
    private const string TargetFramework = "net10.0";
    private IVariableProvider variableProvider;
    private ILogger<ProjectFileUpdater> logger;
    private IFileSystem fileSystem;
    private ProjectFileUpdater projectFileUpdater;
    private List<string> logMessages;

    [SetUp]
    public void Setup()
    {
        ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestCaseAttribute>();
        var sp = ConfigureServices();

        this.logMessages = [];
        this.logger = new TestLogger<ProjectFileUpdater>(this.logMessages.Add);

        this.fileSystem = sp.GetRequiredService<IFileSystem>();
        this.variableProvider = sp.GetRequiredService<IVariableProvider>();
        this.projectFileUpdater = new ProjectFileUpdater(this.logger, this.fileSystem);
    }

    [TearDown]
    public void Teardown() => this.projectFileUpdater.Dispose();

    [TestCase("Microsoft.NET.Sdk")]
    [TestCase("Microsoft.NET.Sdk.Worker")]
    [TestCase("Microsoft.NET.Sdk.Web")]
    [TestCase("Microsoft.NET.Sdk.WindowsDesktop")]
    [TestCase("Microsoft.NET.Sdk.Razor")]
    [TestCase("Microsoft.NET.Sdk.BlazorWebAssembly")]
    [TestCase("Microsoft.Build.Sql")]
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
        logMessages[0].ShouldContain("Specified project file Sdk (SomeOtherProject.Sdk) is not supported, please ensure the project sdk starts with 'Microsoft.NET.Sdk' or 'Microsoft.Build.Sql'");
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
        logMessages[0].ShouldContain("Specified project file Sdk () is not supported, please ensure the project sdk starts with 'Microsoft.NET.Sdk' or 'Microsoft.Build.Sql'");
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
            SemanticVersion.Parse("2.0.0", RegexPatterns.Configuration.DefaultTagPrefixRegexPattern), EmptyConfigurationBuilder.New.Build(), 0
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
        var variables = this.variableProvider.GetVariablesFor(SemanticVersion.Parse("2.0.0", RegexPatterns.Configuration.DefaultTagPrefixRegexPattern), EmptyConfigurationBuilder.New.Build(), 0);
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
                 <PropertyGroup>
                   <AssemblyVersion>1.0.0.0</AssemblyVersion>
                 </PropertyGroup>
               </Project>
               """
    )]
    public void UpdateProjectXmlVersionElementWithDuplicatePropertyGroupsModifiesLastElement(string xml)
    {
        var variables = this.variableProvider.GetVariablesFor(SemanticVersion.Parse("2.0.0", RegexPatterns.Configuration.DefaultTagPrefixRegexPattern), EmptyConfigurationBuilder.New.Build(), 0);
        var xmlRoot = XElement.Parse(xml);
        variables.AssemblySemVer.ShouldNotBeNull();
        ProjectFileUpdater.UpdateProjectVersionElement(xmlRoot, ProjectFileUpdater.AssemblyVersionElement, variables.AssemblySemVer);

        const string projectFileContent = $"""
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
        var variables = this.variableProvider.GetVariablesFor(SemanticVersion.Parse("2.0.0", RegexPatterns.Configuration.DefaultTagPrefixRegexPattern), EmptyConfigurationBuilder.New.Build(), 0);
        var xmlRoot = XElement.Parse(xml);
        variables.AssemblySemVer.ShouldNotBeNull();
        ProjectFileUpdater.UpdateProjectVersionElement(xmlRoot, ProjectFileUpdater.AssemblyVersionElement, variables.AssemblySemVer);

        const string projectFileContent = $"""
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

    [TestCase("Microsoft.NET.Sdk", "TestProject.csproj")]
    [TestCase("Microsoft.Build.Sql", "TestProject.sqlproj")]
    public void UpdateProjectFileAddsVersionToFile(string sdk, string projectName)
    {
        var xml = $"""
                  <Project Sdk="{sdk}">
                    <PropertyGroup>
                      <TargetFramework>net10.0</TargetFramework>
                    </PropertyGroup>
                  </Project>
                  """;
        var workingDirectory = FileSystemHelper.Path.GetTempPath();
        var fileName = FileSystemHelper.Path.Combine(workingDirectory, projectName);

        VerifyAssemblyInfoFile(xml, fileName, AssemblyVersioningScheme.MajorMinorPatch, (fs, variables) =>
        {
            using var projFileUpdater = new ProjectFileUpdater(this.logger, fs);
            projFileUpdater.Execute(variables, new(workingDirectory, false, fileName));

            var expectedXml = $"""
                               <Project Sdk="{sdk}">
                                 <PropertyGroup>
                                   <TargetFramework>{TargetFramework}</TargetFramework>
                                   <AssemblyVersion>2.3.1.0</AssemblyVersion>
                                   <FileVersion>2.3.1.0</FileVersion>
                                   <InformationalVersion>2.3.1+3.Branch.foo.Sha.hash</InformationalVersion>
                                   <Version>2.3.1</Version>
                                 </PropertyGroup>
                               </Project>
                               """;
            var transformedXml = fs.File.ReadAllText(fileName);
            transformedXml.ShouldBe(XElement.Parse(expectedXml).ToString());
        });
    }

    private void VerifyAssemblyInfoFile(
        string projectFileContent,
        string fileName,
        AssemblyVersioningScheme versioningScheme = AssemblyVersioningScheme.MajorMinorPatch,
        Action<IFileSystem, GitVersionVariables>? verify = null)
    {
        var file = Substitute.For<IFile>();
        var versionSourceSemVer = new SemanticVersion(1, 2, 2);

        var version = new SemanticVersion
        {
            BuildMetaData = new(versionSourceSemVer, "versionSourceHash", 3, "foo", "hash", "shortHash", DateTimeOffset.Now, 0, VersionField.Major),
            Major = 2,
            Minor = 3,
            Patch = 1
        };

        file.Exists(fileName).Returns(true);
        file.ReadAllText(fileName).Returns(projectFileContent);
        file.When(f => f.WriteAllText(fileName, Arg.Any<string>())).Do(c =>
        {
            projectFileContent = c.ArgAt<string>(1);
            file.ReadAllText(fileName).Returns(projectFileContent);
        });

        var configuration = EmptyConfigurationBuilder.New.WithAssemblyVersioningScheme(versioningScheme).Build();
        var variables = this.variableProvider.GetVariablesFor(version, configuration, 0);

        this.fileSystem = Substitute.For<IFileSystem>();
        this.fileSystem.File.Returns(file);
        this.fileSystem.FileInfo.Returns(new FileSystem().FileInfo);
        verify?.Invoke(this.fileSystem, variables);
    }
}
