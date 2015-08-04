using System;
using GitVersion;
using NUnit.Framework;
using Shouldly;

[TestFixture]
public class EnvironmentVariableJenkinsTests
{
    string key = "JENKINS_URL";

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
}