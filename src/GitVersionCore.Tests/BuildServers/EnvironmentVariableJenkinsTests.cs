using System;
using GitVersion;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class EnvironmentVariableJenkinsTests
{
    string key = "JENKINS_URL";
    string branch = "GIT_BRANCH";
    string localBranch = "GIT_LOCAL_BRANCH";

    private void SetEnvironmentVariableForDetection()
    {
        Environment.SetEnvironmentVariable(key, "a value", EnvironmentVariableTarget.Process);
    }

    private void ClearEnvironmentVariableForDetection()
    {
        Environment.SetEnvironmentVariable(key, null, EnvironmentVariableTarget.Process);
    }

    [Test]
    public void CanApplyCurrentContextWhenEnvironmentVariableIsSet()
    {
        SetEnvironmentVariableForDetection();
        var j = new Jenkins();
        j.CanApplyToCurrentContext().ShouldBe(true);
    }
    
    [Test]
    public void CanNotApplyCurrentContextWhenEnvironmentVariableIsNotSet()
    {
        ClearEnvironmentVariableForDetection();
        var j = new Jenkins();
        j.CanApplyToCurrentContext().ShouldBe(false);  
    }

    [Test]
    public void JenkinsTakesLocalBranchNameNotRemoteName()
    {
        // Save original values so they can be restored
        string branchOrig = Environment.GetEnvironmentVariable(branch);
        string localBranchOrig = Environment.GetEnvironmentVariable(localBranch);

        // Set GIT_BRANCH for testing
        Environment.SetEnvironmentVariable(branch, "origin/master");

        // Test Jenkins that GetCurrentBranch falls back to GIT_BRANCH if GIT_LOCAL_BRANCH undefined
        var j = new Jenkins();
        j.GetCurrentBranch(true).ShouldBe("origin/master");

        // Set GIT_LOCAL_BRANCH
        Environment.SetEnvironmentVariable(localBranch, "master");

        // Test Jenkins GetCurrentBranch method now returns GIT_LOCAL_BRANCH
        j.GetCurrentBranch(true).ShouldBe("master");

        // Restore environment variables
        Environment.SetEnvironmentVariable(branch, branchOrig);
        Environment.SetEnvironmentVariable(localBranch, localBranchOrig);
    }
}