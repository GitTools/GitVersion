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
	/u	Url to remote git repository.
	/b	Name of the branch to use on the remote repository, must be used in combination with /u.
";
            Console.Write(message);
        }
    }
}