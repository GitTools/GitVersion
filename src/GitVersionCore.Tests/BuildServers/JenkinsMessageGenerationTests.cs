using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Shouldly;
using GitVersion.BuildServers;
using GitVersion;
using GitVersion.Logging;
using GitVersion.OutputVariables;
using GitVersion.VersionCalculation;

namespace GitVersionCore.Tests.BuildServers
{
    [TestFixture]
    public class JenkinsMessageGenerationTests : TestBase
    {
        private IEnvironment environment;
        private ILog log;
        private IVariableProvider variableProvider;

        [SetUp]
        public void SetUp()
        {
            environment = new TestEnvironment();
            log = new NullLog();
            var metaDataCalculator = new MetaDataCalculator();
            var baseVersionCalculator = new BaseVersionCalculator(log, null);
            var mainlineVersionCalculator = new MainlineVersionCalculator(log, metaDataCalculator);
            var nextVersionCalculator = new NextVersionCalculator(log, metaDataCalculator, baseVersionCalculator, mainlineVersionCalculator);
            variableProvider = new VariableProvider(nextVersionCalculator, new TestEnvironment());
        }

        [Test]
        public void GenerateSetVersionMessageReturnsVersionAsIsAlthoughThisIsNotUsedByJenkins()
        {
            var j = new Jenkins(environment, log);
            var vars = new TestableVersionVariables(fullSemVer: "0.0.0-Beta4.7");
            j.GenerateSetVersionMessage(vars).ShouldBe("0.0.0-Beta4.7");
        }

        [Test]
        public void GenerateMessageTest()
        {
            var j = new Jenkins(environment, log);
            var generatedParameterMessages = j.GenerateSetParameterMessage("name", "value");
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

        private void AssertVariablesAreWrittenToFile(string f)
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

            var variables = variableProvider.GetVariablesFor(semanticVersion, config, false);

            var j = new Jenkins(environment, log, f);

            j.WriteIntegration(writes.Add, variables);

            writes[1].ShouldBe("1.2.3-beta.1+5");

            File.Exists(f).ShouldBe(true);

            var props = File.ReadAllText(f);

            props.ShouldContain("GitVersion_Major=1");
            props.ShouldContain("GitVersion_Minor=2");
        }
    }
}
