using System.Collections.Concurrent;
using GitVersion.MsBuild.Tests.Helpers;
using Microsoft.Build.Framework;
using Shouldly;

namespace GitVersion.MsBuild.Tests.Mocks;

internal sealed class MockEngine : IBuildEngine4
{
    private readonly ConcurrentDictionary<object, object> objectCache = new();
    private StringBuilder log = new();

    internal MessageImportance MinimumMessageImportance { get; set; } = MessageImportance.Low;

    internal int Messages { set; get; }

    internal int Warnings { set; get; }

    internal int Errors { set; get; }

    public bool IsRunningMultipleNodes { get; set; }

    public void LogErrorEvent(BuildErrorEventArgs e)
    {
        Console.WriteLine(EventArgsFormatting.FormatEventMessage(e));
        this.log.AppendLine(EventArgsFormatting.FormatEventMessage(e));
        ++Errors;
    }

    public void LogWarningEvent(BuildWarningEventArgs e)
    {
        Console.WriteLine(EventArgsFormatting.FormatEventMessage(e));
        this.log.AppendLine(EventArgsFormatting.FormatEventMessage(e));
        ++Warnings;
    }

    public void LogCustomEvent(CustomBuildEventArgs e)
    {
        Console.WriteLine(e.Message);
        this.log.AppendLine(e.Message);
    }

    public void LogMessageEvent(BuildMessageEventArgs e)
    {
        // Only if the message is above the minimum importance should we record the log message
        if (e.Importance <= MinimumMessageImportance)
        {
            Console.WriteLine(e.Message);
            this.log.AppendLine(e.Message);
            ++Messages;
        }
    }

    public bool ContinueOnError => false;

    public string ProjectFileOfTaskNode => string.Empty;

    public int LineNumberOfTaskNode => 0;

    public int ColumnNumberOfTaskNode => 0;

    public string Log
    {
        set => this.log = new StringBuilder(value);
        get => this.log.ToString();
    }

    public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => false;

    public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs, string toolsVersion) => false;

    /// <summary>
    /// Assert that the log file contains the given string.
    /// Case insensitive.
    /// </summary>
    /// <param name="contains"></param>
    internal void AssertLogContains(string contains) => Log.ShouldContain(contains, Case.Insensitive);

    /// <summary>
    /// Assert that the log doesn't contain the given string.
    /// </summary>
    /// <param name="contains"></param>
    internal void AssertLogDoesntContain(string contains) => Log.ShouldNotContain(contains, Case.Insensitive);

    public bool BuildProjectFilesInParallel(
        string[] projectFileNames,
        string[] targetNames,
        IDictionary[] globalProperties,
        IDictionary[] targetOutputsPerProject,
        string[] toolsVersion,
        bool useResultsCache,
        bool unloadProjectsOnCompletion) => false;


    public BuildEngineResult BuildProjectFilesInParallel(
        string[] projectFileNames,
        string[] targetNames,
        IDictionary[] globalProperties,
        IList<string>[] removeGlobalProperties,
        string[] toolsVersion,
        bool returnTargetOutputs) => new(false, null);

    public void Yield()
    {
    }

    public void Reacquire()
    {
    }

    public object GetRegisteredTaskObject(object key, RegisteredTaskObjectLifetime lifetime)
    {
        this.objectCache.TryGetValue(key, out var obj);
        return obj;
    }

    public void RegisterTaskObject(object key, object obj, RegisteredTaskObjectLifetime lifetime, bool allowEarlyCollection) => this.objectCache[key] = obj;

    public object UnregisterTaskObject(object key, RegisteredTaskObjectLifetime lifetime)
    {
        this.objectCache.TryRemove(key, out var obj);
        return obj;
    }
}
