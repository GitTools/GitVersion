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
    public class JenkinsTests : TestBase
    {
        private const string key = "JENKINS_URL";
        private const string branch = "GIT_BRANCH";
        private const string localBranch = "GIT_LOCAL_BRANCH";
        private const string pipelineBranch = "BRANCH_NAME";
        private IEnvironment environment;
        private IServiceProvider sp;
        private Jenkins buildServer;

        [SetUp]
        public void SetUp()
        {
            sp = ConfigureServices(services =>
            {
                services.AddSingleton<Jenkins>();
            });
            environment = sp.GetService<IEnvironment>();
            buildServer = sp.GetService<Jenkins>();
        }

        private void SetEnvironmentVariableForDetection()
        {
            environment.SetEnvironmentVariable(key, "a value");
        }

        private void ClearenvironmentVariableForDetection()
        {
            environment.SetEnvironmentVariable(key, null);
        }

        [Test]
        public void CanApplyCurrentContextWhenenvironmentVariableIsSet()
        {
            SetEnvironmentVariableForDetection();
            buildServer.CanApplyToCurrentContext().ShouldBe(true);
        }

        [Test]
        public void CanNotApplyCurrentContextWhenenvironmentVariableIsNotSet()
        {
            ClearenvironmentVariableForDetection();
            buildServer.CanApplyToCurrentContext().ShouldBe(false);
        }

        [Test]
        public void JenkinsTakesLocalBranchNameNotRemoteName()
        {
            // Save original values so they can be restored
            var branchOrig = environment.GetEnvironmentVariable(branch);
            var localBranchOrig = environment.GetEnvironmentVariable(localBranch);

            // Set GIT_BRANCH for testing
            environment.SetEnvironmentVariable(branch, "origin/master");

            // Test Jenkins that GetCurrentBranch falls back to GIT_BRANCH if GIT_LOCAL_BRANCH undefined
            buildServer.GetCurrentBranch(true).ShouldBe("origin/master");

            // Set GIT_LOCAL_BRANCH
            environment.SetEnvironmentVariable(localBranch, "master");

            // Test Jenkins GetCurrentBranch method now returns GIT_LOCAL_BRANCH
            buildServer.GetCurrentBranch(true).ShouldBe("master");

            // Restore environment variables
            environment.SetEnvironmentVariable(branch, branchOrig);
            environment.SetEnvironmentVariable(localBranch, localBranchOrig);
        }

        [Test]
        public void JenkinsTakesBranchNameInPipelineAsCode()
        {
            // Save original values so they can be restored
            var branchOrig = environment.GetEnvironmentVariable(branch);
            var localBranchOrig = environment.GetEnvironmentVariable(localBranch);
            var pipelineBranchOrig = environment.GetEnvironmentVariable(pipelineBranch);

            // Set BRANCH_NAME in pipeline mode
            environment.SetEnvironmentVariable(pipelineBranch, "master");
            // When Jenkins uses a Pipeline, GIT_BRANCH and GIT_LOCAL_BRANCH are not set:
            environment.SetEnvironmentVariable(branch, null);
            environment.SetEnvironmentVariable(localBranch, null);

            // Test Jenkins GetCurrentBranch method now returns BRANCH_NAME
            buildServer.GetCurrentBranch(true).ShouldBe("master");

            // Restore environment variables
            environment.SetEnvironmentVariable(branch, branchOrig);
            environment.SetEnvironmentVariable(localBranch, localBranchOrig);
            environment.SetEnvironmentVariable(pipelineBranch, pipelineBranchOrig);
        }

        [Test]
        public void GenerateSetVersionMessageReturnsVersionAsIsAlthoughThisIsNotUsedByJenkins()
        {
            var vars = new TestableVersionVariables(fullSemVer: "0.0.0-Beta4.7");
            buildServer.GenerateSetVersionMessage(vars).ShouldBe("0.0.0-Beta4.7");
        }

        [Test]
        public void GenerateMessageTest()
        {
            var generatedParameterMessages = buildServer.GenerateSetParameterMessage("name", "value");
            generatedParameterMessages.Length.ShouldBe(1);
            generatedParameterMessages[0].ShouldBe("GitVersion_name=value");
        }

        [Test]
        public void WriteAllVariablesToTheTextWriter()
        {
            var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var f = Path.Combine(assemblyLocation, "gitlab_this_file_should_be_deleted.properties");

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
