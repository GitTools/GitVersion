using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using GitVersionTask.Tests.Helpers;
using Microsoft.Build.Framework;
using Shouldly;

namespace GitVersionTask.Tests.Mocks
{
    internal sealed class MockEngine : IBuildEngine4
    {
        private readonly ConcurrentDictionary<object, object> _objectCache = new ConcurrentDictionary<object, object>();
        private StringBuilder _log = new StringBuilder();

        internal MessageImportance MinimumMessageImportance { get; set; } = MessageImportance.Low;

        internal int Messages { set; get; }

        internal int Warnings { set; get; }

        internal int Errors { set; get; }

        public bool IsRunningMultipleNodes { get; set; }

        public void LogErrorEvent(BuildErrorEventArgs eventArgs)
        {
            Console.WriteLine(EventArgsFormatting.FormatEventMessage(eventArgs));
            _log.AppendLine(EventArgsFormatting.FormatEventMessage(eventArgs));
            ++Errors;
        }

        public void LogWarningEvent(BuildWarningEventArgs eventArgs)
        {
            Console.WriteLine(EventArgsFormatting.FormatEventMessage(eventArgs));
            _log.AppendLine(EventArgsFormatting.FormatEventMessage(eventArgs));
            ++Warnings;
        }

        public void LogCustomEvent(CustomBuildEventArgs eventArgs)
        {
            Console.WriteLine(eventArgs.Message);
            _log.AppendLine(eventArgs.Message);
        }

        public void LogMessageEvent(BuildMessageEventArgs eventArgs)
        {
            // Only if the message is above the minimum importance should we record the log message
            if (eventArgs.Importance <= MinimumMessageImportance)
            {
                Console.WriteLine(eventArgs.Message);
                _log.AppendLine(eventArgs.Message);
                ++Messages;
            }
        }

        public bool ContinueOnError => false;

        public string ProjectFileOfTaskNode => string.Empty;

        public int LineNumberOfTaskNode => 0;

        public int ColumnNumberOfTaskNode => 0;

        public string Log
        {
            set => _log = new StringBuilder(value);
            get => _log.ToString();
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
            IList<string>[] undefineProperties,
            string[] toolsVersion,
            bool includeTargetOutputs) => new BuildEngineResult(false, null);

        public void Yield()
        {
        }

        public void Reacquire()
        {
        }

        public object GetRegisteredTaskObject(object key, RegisteredTaskObjectLifetime lifetime)
        {
            _objectCache.TryGetValue(key, out var obj);
            return obj;
        }

        public void RegisterTaskObject(object key, object obj, RegisteredTaskObjectLifetime lifetime, bool allowEarlyCollection)
        {
            _objectCache[key] = obj;
        }

        public object UnregisterTaskObject(object key, RegisteredTaskObjectLifetime lifetime)
        {
            _objectCache.TryRemove(key, out var obj);
            return obj;
        }
    }
}
