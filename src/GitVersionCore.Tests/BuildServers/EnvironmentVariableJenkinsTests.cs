﻿using System;
using GitVersion;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class EnvironmentVariableJenkinsTests
{
    string key = "JENKINS_URL";
    string branch = "GIT_BRANCH";
    string localBranch = "GIT_LOCAL_BRANCH";
    string pipelineBranch = "BRANCH_NAME";

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

    [Test]
    public void JenkinsTakesBranchNameInPipelineAsCode()
    {
        // Save original values so they can be restored
        string branchOrig = Environment.GetEnvironmentVariable(branch);
        string localBranchOrig = Environment.GetEnvironmentVariable(localBranch);
        string pipelineBranchOrig = Environment.GetEnvironmentVariable(pipelineBranch);

        // Set BRANCH_NAME in pipeline mode
        Environment.SetEnvironmentVariable(pipelineBranch, "master");
        // When Jenkins uses a Pipeline, GIT_BRANCH and GIT_LOCAL_BRANCH are not set:
        Environment.SetEnvironmentVariable(branch, null);
        Environment.SetEnvironmentVariable(localBranch, null);

        // Test Jenkins GetCurrentBranch method now returns BRANCH_NAME
        var j = new Jenkins();
        j.GetCurrentBranch(true).ShouldBe("master");

        // Restore environment variables
        Environment.SetEnvironmentVariable(branch, branchOrig);
        Environment.SetEnvironmentVariable(localBranch, localBranchOrig);
        Environment.SetEnvironmentVariable(pipelineBranch, pipelineBranchOrig);
    }
}