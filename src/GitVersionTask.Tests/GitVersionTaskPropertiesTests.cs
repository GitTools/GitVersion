namespace GitVersionTask.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using Microsoft.Build.Evaluation;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Logging;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    public class GitVersionTaskPropertiesTests
    {


        static string CreateProjectXml()
        {
            var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return $@"
            <Project xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
                <PropertyGroup>
                    <TargetFramework>net461</TargetFramework>
                    <Configuration>Debug</Configuration>
                    <GitVersionTaskRoot>{currentDirectory}\..\..\..\..\GitVersionTask\build\</GitVersionTaskRoot>
                    <GitVersionAssemblyFile>{currentDirectory}\..\..\GitVersionTask.MsBuild\bin\$(Configuration)\$(TargetFramework)\GitVersionTask.MsBuild.dll</GitVersionAssemblyFile>
                </PropertyGroup>
                <Import Project='$(GitVersionTaskRoot)GitVersionTask.props'/>
                <Import Project='$(GitVersionTaskRoot)GitVersionTask.targets'/>
            </Project>";
        }

        (bool failed, Project project) CallMsBuild(IDictionary<string, string> globalProperties)
        {
            using (var stringReader = new StringReader(CreateProjectXml()))
            {
                StringBuilder builder;
                Project project;
                bool result;
                using (var collection = new ProjectCollection(globalProperties))
                {
                    builder = new StringBuilder();
                    var writer = new StringWriter(builder);
                    WriteHandler handler = x => writer.WriteLine(x);
                    var logger = new ConsoleLogger(LoggerVerbosity.Quiet, handler, null, null);
                    collection.RegisterLogger(logger);
                    XmlReader reader = new XmlTextReader(stringReader);
                    project = collection.LoadProject(reader);
                    result = project.Build();
                    collection.UnregisterAllLoggers();
                }

                if (!result)
                {
                    var consoleOutput = builder.ToString();
                    TestContext.Error.Write(consoleOutput);
                }

                return (!result, project);
            }
        }

        [Test]
        [Category("NoMono")]
        public void Zero_properties_should_fail_the_build_because()
        {
            var result = CallMsBuild(new Dictionary<string, string>());
            result.failed.ShouldBeTrue();
        }

        [Test]
        [Category("NoMono")]
        public void With_DisableGitVersionTask_the_build_should_work()
        {
            var globalProperties = new Dictionary<string, string>
            {
                {"DisableGitVersionTask", "true"}
            };
            var result = CallMsBuild(globalProperties);
            result.failed.ShouldBeFalse();
        }

        [Test]
        [Category("NoMono")]
        public void With_Enabled_GetVersionTask_the_Build_properties_should_be_initialized()
        {
            var result = CallMsBuild(new Dictionary<string, string>());
            PropertyValueShouldBe(result, "DisableGitVersionTask", "false");
            PropertyValueShouldBe(result, "WriteVersionInfoToBuildLog", "true");
            PropertyValueShouldBe(result, "UpdateAssemblyInfo", "true");
            PropertyValueShouldBe(result, "GenerateGitVersionInformation", "true");
            PropertyValueShouldBe(result, "GetVersion", "true");
            PropertyValueShouldBe(result, "UpdateVersionProperties", "true");
        }



        [Test]
        [Category("NoMono")]
        public void With_DisabledGetVersionTask_the_WriteVersionInfoToBuildLog_should_be_false()
        {
            var globalProperties = new Dictionary<string, string>
            {
                {"DisableGitVersionTask", "true"}
            };
            var result = CallMsBuild(globalProperties);
            PropertyValueShouldBe(result, "DisableGitVersionTask", "true");
            PropertyValueShouldBe(result, "WriteVersionInfoToBuildLog", "false");
        }

        [Test]
        [Category("NoMono")]
        public void With_DisabledGetVersionTask_the_UpdateAssemblyInfo_should_be_false()
        {
            var globalProperties = new Dictionary<string, string>
            {
                {"DisableGitVersionTask", "true"}
            };
            var result = CallMsBuild(globalProperties);
            PropertyValueShouldBe(result, "UpdateAssemblyInfo", "false");
        }

        [Test]
        [Category("NoMono")]
        public void With_DisabledGetVersionTask_the_GenerateGitVersionInformation_should_be_false()
        {
            var globalProperties = new Dictionary<string, string>
            {
                {"DisableGitVersionTask", "true"}
            };
            var result = CallMsBuild(globalProperties);
            PropertyValueShouldBe(result, "GenerateGitVersionInformation", "false");
        }

        [Test]
        [Category("NoMono")]
        public void With_DisabledGetVersionTask_the_GetVersion_should_be_false()
        {
            var globalProperties = new Dictionary<string, string>
            {
                {"DisableGitVersionTask", "true"}
            };
            var result = CallMsBuild(globalProperties);
            PropertyValueShouldBe(result, "GetVersion", "false");
        }

        [Test]
        [Category("NoMono")]
        public void With_DisabledGetVersionTask_the_UpdateVersionProperties_should_be_false()
        {
            var globalProperties = new Dictionary<string, string>
            {
                {"DisableGitVersionTask", "true"}
            };
            var result = CallMsBuild(globalProperties);

            PropertyValueShouldBe(result, "UpdateVersionProperties", "false");
        }


        static void PropertyValueShouldBe((bool failed, Project project) result, string propertyName, string expectedValue)
        {
            var property = result.project.Properties.First(p => p.Name == propertyName);

            property.EvaluatedValue.ShouldBe(expectedValue);
        }
    }
}
