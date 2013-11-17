namespace GitFlowVersion
{
    using System;

    class HelpWriter
    {
        public static void Write()
        {
            var message =
@"Use convention to derive a SemVer product version from a GitFlow based repository.

GitFlowVersion [path] [/l logFilePath]

	path	The directory containing .git. If not defined current directory is used.
	/l	Path to logfile.
";
            Console.Write(message);
        }
    }
}