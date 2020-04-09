using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GitVersion;
using GitVersion.BuildAgents;
using GitVersion.VersionCalculation;
using GitVersionCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;

namespace GitVersionCore.Tests.BuildAgents
{
    [TestFixture]
    public sealed class CodeBuildTests : TestBase
    {
        private IEnvironment environment;
        private IServiceProvider sp;
        private CodeBuild buildServer;

        [SetUp]
        public void SetUp()
        {
            sp = ConfigureServices(services =>
            {
                services.AddSingleton<CodeBuild>();
            });
            environment = sp.GetService<IEnvironment>();
            buildServer = sp.GetService<CodeBuild>();
        }

        [Test]
        public void CorrectlyIdentifiesCodeBuildPresence()
        {
            environment.SetEnvironmentVariable(CodeBuild.EnvironmentVariableName, "a value");
            buildServer.CanApplyToCurrentContext().ShouldBe(true);
        }

        [Test]
        public void PicksUpBranchNameFromEnvironment()
        {
            environment.SetEnvironmentVariable(CodeBuild.EnvironmentVariableName, "refs/heads/master");
            buildServer.GetCurrentBranch(false).ShouldBe("refs/heads/master");
        }

        [Test]
        public void WriteAllVariablesToTheTextWriter()
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var f = Path.Combine(assemblyLocation, "codebuild_this_file_should_be_deleted.properties");

            try
            {
                AssertVariablesAreWrittenToFile(f);
            }
            finally
            {
                File.Delete(f);
            }
        }

        private void AssertVariablesAreWrittenToFile(string file)
        {
            var writes = new List<string>();
            var semanticVersion = new SemanticVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 3,
                PreReleaseTag = "beta1",
                BuildMetaData = "5"
            };

            semanticVersion.BuildMetaData.CommitDate = DateTimeOffset.Parse("2014-03-06 23:59:59Z");
            semanticVersion.BuildMetaData.Sha = "commitSha";

            var config = new TestEffectiveConfiguration();

            var variableProvider = sp.GetService<IVariableProvider>();

            var variables = variableProvider.GetVariablesFor(semanticVersion, config, false);

            buildServer.WithPropertyFile(file);

            buildServer.WriteIntegration(writes.Add, variables);

            writes[1].ShouldBe("1.2.3-beta.1+5");

            File.Exists(file).ShouldBe(true);

            var props = File.ReadAllText(file);

            props.ShouldContain("GitVersion_Major=1");
            props.ShouldContain("GitVersion_Minor=2");
        }
    }
}
