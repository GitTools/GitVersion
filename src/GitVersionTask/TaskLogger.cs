using System;
using System.IO;

class TaskLogger
{
    private readonly TextWriter stdout;
    private readonly TextWriter stderr;

    public TaskLogger(TextWriter paramStdout = null, TextWriter paramStderr = null)
    {
        this.stdout = paramStdout ?? Console.Out;
        this.stderr = paramStderr ?? Console.Error;
    }

    public void LogWarning(string message)
    {
        this.stdout.WriteLine(message);
    }

    public void LogInfo(string message)
    {
        this.stdout.WriteLine(message);
    }

    public void LogError(string message)
    {
        this.stderr.WriteLine(message);
    }
}
