using NUnit.Framework;
using Shouldly;
using GitVersion.BuildServers;
using GitVersion;
using GitVersion.Logging;

namespace GitVersionCore.Tests.BuildServers
{
    [TestFixture]
    public class EnvironmentVariableJenkinsTests : TestBase
    {
        private readonly string key = "JENKINS_URL";
        private readonly string branch = "GIT_BRANCH";
        private readonly string localBranch = "GIT_LOCAL_BRANCH";
        private readonly string pipelineBranch = "BRANCH_NAME";
        private IEnvironment environment;
        private ILog log;

        [SetUp]
        public void SetUp()
        {
            environment = new TestEnvironment();
            log = new NullLog();
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
            var j = new Jenkins(environment, log);
            j.CanApplyToCurrentContext().ShouldBe(true);
        }
    
        [Test]
        public void CanNotApplyCurrentContextWhenenvironmentVariableIsNotSet()
        {
            ClearenvironmentVariableForDetection();
            var j = new Jenkins(environment, log);
            j.CanApplyToCurrentContext().ShouldBe(false);  
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
            var j = new Jenkins(environment, log);
            j.GetCurrentBranch(true).ShouldBe("origin/master");

            // Set GIT_LOCAL_BRANCH
            environment.SetEnvironmentVariable(localBranch, "master");

            // Test Jenkins GetCurrentBranch method now returns GIT_LOCAL_BRANCH
            j.GetCurrentBranch(true).ShouldBe("master");

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
            var j = new Jenkins(environment, log);
            j.GetCurrentBranch(true).ShouldBe("master");

            // Restore environment variables
            environment.SetEnvironmentVariable(branch, branchOrig);
            environment.SetEnvironmentVariable(localBranch, localBranchOrig);
            environment.SetEnvironmentVariable(pipelineBranch, pipelineBranchOrig);
        }
    }
}
