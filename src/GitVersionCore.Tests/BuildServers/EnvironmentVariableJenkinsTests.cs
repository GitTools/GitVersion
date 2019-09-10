using NUnit.Framework;
using Shouldly;
using GitVersion.BuildServers;
using GitVersion.Common;

namespace GitVersionCore.Tests.BuildServers
{
    [TestFixture]
    public class EnvironmentVariableJenkinsTests : TestBase
    {
        string key = "JENKINS_URL";
        string branch = "GIT_BRANCH";
        string localBranch = "GIT_LOCAL_BRANCH";
        string pipelineBranch = "BRANCH_NAME";
        private IEnvironment environment;

        [SetUp]
        public void SetUp()
        {
            environment = new TestEnvironment();
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
            var j = new Jenkins(environment);
            j.CanApplyToCurrentContext().ShouldBe(true);
        }
    
        [Test]
        public void CanNotApplyCurrentContextWhenenvironmentVariableIsNotSet()
        {
            ClearenvironmentVariableForDetection();
            var j = new Jenkins(environment);
            j.CanApplyToCurrentContext().ShouldBe(false);  
        }

        [Test]
        public void JenkinsTakesLocalBranchNameNotRemoteName()
        {
            // Save original values so they can be restored
            string branchOrig = environment.GetEnvironmentVariable(branch);
            string localBranchOrig = environment.GetEnvironmentVariable(localBranch);

            // Set GIT_BRANCH for testing
            environment.SetEnvironmentVariable(branch, "origin/master");

            // Test Jenkins that GetCurrentBranch falls back to GIT_BRANCH if GIT_LOCAL_BRANCH undefined
            var j = new Jenkins(environment);
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
            string branchOrig = environment.GetEnvironmentVariable(branch);
            string localBranchOrig = environment.GetEnvironmentVariable(localBranch);
            string pipelineBranchOrig = environment.GetEnvironmentVariable(pipelineBranch);

            // Set BRANCH_NAME in pipeline mode
            environment.SetEnvironmentVariable(pipelineBranch, "master");
            // When Jenkins uses a Pipeline, GIT_BRANCH and GIT_LOCAL_BRANCH are not set:
            environment.SetEnvironmentVariable(branch, null);
            environment.SetEnvironmentVariable(localBranch, null);

            // Test Jenkins GetCurrentBranch method now returns BRANCH_NAME
            var j = new Jenkins(environment);
            j.GetCurrentBranch(true).ShouldBe("master");

            // Restore environment variables
            environment.SetEnvironmentVariable(branch, branchOrig);
            environment.SetEnvironmentVariable(localBranch, localBranchOrig);
            environment.SetEnvironmentVariable(pipelineBranch, pipelineBranchOrig);
        }
    }
}
