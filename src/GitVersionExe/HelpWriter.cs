namespace GitVersion
{
    using System;

    class HelpWriter
    {
        public static void Write()
        {
            WriteTo(Console.WriteLine);
        }

        public static void WriteTo(Action<string> writeAction)
        {
            const string message = @"Use convention to derive a SemVer product version from a GitFlow or GitHub based repository.

GitVersion [path]

    path            The directory containing .git. If not defined current directory is used. (Must be first argument)
    init            Configuration utility for gitversion
    /h or /?        Shows Help

    /targetpath     Same as 'path', but not positional
    /output         Determines the output to the console. Can be either 'json' or 'buildserver', will default to 'json'.
    /showvariable   Used in conjuntion with /output json, will output just a particular variable. 
                    eg /output json /showvariable SemVer - will output `1.2.3+beta.4`
    /l              Path to logfile.
    /showconfig     Outputs the effective GitVersion config (defaults + custom from GitVersion.yml) in yaml format
    /overrideconfig Overrides GitVersion config values inline (semicolon-separated key value pairs e.g. /overrideconfig tag-prefix=Foo)
                    Currently supported config overrides: tag-prefix

    # AssemblyInfo updating
    /updateassemblyinfo
                    Will recursively search for all 'AssemblyInfo.cs' files in the git repo and update them
    /updateassemblyinfofilename
                    Specify name of AssemblyInfo file. Can also /updateAssemblyInfo GlobalAssemblyInfo.cs as a shorthand
    /ensureassemblyinfo
                    If the assembly info file specified with /updateassemblyinfo or /updateassemblyinfofilename is not found, 
                    it be created with these attributes: AssemblyFileVersion, AssemblyVersion and AssemblyInformationalVersion
                    ---        
                    Supports writing version info for: C#, F#, VB    
    # Remote repository args
    /url            Url to remote git repository.
    /b              Name of the branch to use on the remote repository, must be used in combination with /url.
    /u              Username in case authentication is required.
    /p              Password in case authentication is required.
    /c              The commit id to check. If not specified, the latest available commit on the specified branch will be used.
    /dynamicRepoLocation
                    By default dynamic repositories will be cloned to %tmp%. Use this switch to override
    /nofetch        Disables 'git fetch' during version calculation. Might cause GitVersion to not calculate your version as expected.
    
    # Execute build args
    /exec           Executes target executable making GitVersion variables available as environmental variables
    /execargs       Arguments for the executable specified by /exec
    /proj           Build a msbuild file, GitVersion variables will be passed as msbuild properties
    /projargs       Additional arguments to pass to msbuild


gitversion init     Configuration utility for gitversion
";

            writeAction(message);
        }
    }
}