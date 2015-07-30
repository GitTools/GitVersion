using System;
using GitVersion;
using NUnit.Framework;

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
        Assert.True(j.CanApplyToCurrentContext());   
    }
    
    [Test]
    public void CanNotApplyCurrentContextWhenEnvironmentVariableIsNotSet()
    {
        ClearEnvironmentVariableForDetection();
        var j = new Jenkins();
        Assert.False(j.CanApplyToCurrentContext());  
    }
}