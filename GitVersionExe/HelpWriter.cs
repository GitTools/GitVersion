namespace GitVersion
{
    using System;

    class HelpWriter
    {
        public static void Write()
        {
            const string message = @"Use convention to derive a SemVer product version from a GitFlow or GitHub based repository.

GitVersion [path] [/l logFilePath]

    path       The directory containing .git. If not defined current directory is used.
    /url       Url to remote git repository.
    /b         Name of the branch to use on the remote repository, must be used in combination with /url.
    /u         Username in case authentication is required.
    /p         Password in case authentication is required.
    /output    Determines the output to the console. Can be either 'json' or 'buildserver', will default to 'json'.
    /l         Path to logfile.
    /exec      Executes target executable making GitVersion variables available as environmental variables
    /execargs  Arguments for the executable specified by /exec
    /proj      Build a msbuild file, GitVersion variables will be passed as msbuild properties
    /projargs  Additional arguments to pass to msbuild
";
            Console.Write(message);
        }
    }
}