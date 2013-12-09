using System;
using System.Collections;
using Microsoft.Build.Framework;

class MockBuildEngine : IBuildEngine
{
    public void LogErrorEvent(BuildErrorEventArgs e)
    { }

    public void LogWarningEvent(BuildWarningEventArgs e)
    { }

    public void LogMessageEvent(BuildMessageEventArgs e)
    { }

    public void LogCustomEvent(CustomBuildEventArgs e)
    {
        throw new NotImplementedException();
    }

    public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs)
    {
        throw new NotImplementedException();
    }

    public bool ContinueOnError
    {
        get { throw new NotImplementedException(); }
    }

    public int LineNumberOfTaskNode
    {
        get { throw new NotImplementedException(); }
    }

    public int ColumnNumberOfTaskNode
    {
        get { throw new NotImplementedException(); }
    }

    public string ProjectFileOfTaskNode
    {
        get { throw new NotImplementedException(); }
    }
}