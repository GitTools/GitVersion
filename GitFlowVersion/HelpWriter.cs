namespace GitFlowVersion
{
    using System;

    class HelpWriter
    {
        public static void Write()
        {
            var message = 
                @"Use convention to derive a SemVer product version from a GitFlow based repository.
GitFlowVersion [targetDirectory] [/l logFilePath]

 /l\tPath to logfile";
            Console.Write(message);
        }
    }
}