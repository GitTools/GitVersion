using System;
using System.IO;

public class TaskLogger
{
    private readonly TextWriter stdout;
    private readonly TextWriter stderr;

    public TaskLogger(TextWriter paramStdout = null, TextWriter paramStderr = null)
    {
        stdout = paramStdout ?? Console.Out;
        stderr = paramStderr ?? Console.Error;
    }

    public void LogWarning(string message)
    {
        stdout.WriteLine(message);
    }

    public void LogInfo(string message)
    {
        stdout.WriteLine(message);
    }

    public void LogError(string message)
    {
        stderr.WriteLine(message);
    }
}
