```bash
Use convention to derive a SemVer product version from a GitFlow or GitHub based
repository.

GitVersion [path]

    path            The directory containing .git. If not defined current
                    directory is used. (Must be first argument)
    /version        Displays the version of GitVersion
    /diag           Runs GitVersion with additional diagnostic information;
                    also needs the '/l' argument to specify a logfile or stdout
                    (requires git.exe to be installed)
    /h or /?        Shows Help

    /targetpath     Same as 'path', but not positional
    /output         Determines the output to the console. Can be either 'json',
                    'file', 'buildserver' or 'dotenv', will default to 'json'.
    /outputfile     Path to output file. It is used in combination with /output
                    'file'.
    /showvariable   Used in conjunction with /output json, will output just a
                    particular variable. E.g. /output json /showvariable SemVer
                    - will output `1.2.3+beta.4`
    /format         Used in conjunction with /output json, will output a format
                    containing version variables.
                    Supports C# format strings - see [Format Strings](/docs/reference/custom-formatting) for details.
                    E.g. /output json /format {SemVer} - will output `1.2.3+beta.4`
                         /output json /format {Major}.{Minor} - will output `1.2`
    /l              Path to logfile; specify 'console' to emit to stdout.
    /config         Path to config file (defaults to GitVersion.yml, GitVersion.yaml, .GitVersion.yml or .GitVersion.yaml)
    /showconfig     Outputs the effective GitVersion config (defaults + custom
                    from GitVersion.yml, GitVersion.yaml, .GitVersion.yml or .GitVersion.yaml) in yaml format
    /overrideconfig Overrides GitVersion config values inline (semicolon-
                    separated key value pairs e.g. /overrideconfig
                    tag-prefix=Foo)
                    Currently supported config overrides: tag-prefix
    /nocache        Bypasses the cache, result will not be written to the cache.
    /nonormalize    Disables normalize step on a build server.
    /allowshallow   Allows GitVersion to run on a shallow clone.
                    This is not recommended, but can be used if you are sure
                    that the shallow clone contains all the information needed
                    to calculate the version.
    /verbosity      Specifies the amount of information to be displayed.
                    (Quiet, Minimal, Normal, Verbose, Diagnostic)
                    Default is Normal

# AssemblyInfo updating

    /updateassemblyinfo
                    Will recursively search for all 'AssemblyInfo.cs' files in
                    the git repo and update them
    /updateprojectfiles
                    Will recursively search for all project files
                    (.csproj/.vbproj/.fsproj/.sqlproj) files in the git repo and update
                    them
                    Note: This is only compatible with the newer Sdk projects
    /ensureassemblyinfo
                    If the assembly info file specified with
                    /updateassemblyinfo is not
                    found, it will be created with these attributes:
                    AssemblyFileVersion, AssemblyVersion and
                    AssemblyInformationalVersion.
                    Supports writing version info for: C#, F#, VB

# Create or update Wix version file

    /updatewixversionfile
                   All the GitVersion variables are written to
                   'GitVersion_WixVersion.wxi'. The variables can then be
                   referenced in other WiX project files for versioning.

# Remote repository args

    /url            Url to remote git repository.
    /b              Name of the branch to use on the remote repository, must be
                    used in combination with /url.
    /u              Username in case authentication is required.
    /p              Password in case authentication is required.
    /c              The commit id to check. If not specified, the latest
                    available commit on the specified branch will be used.
    /dynamicRepoLocation
                    By default dynamic repositories will be cloned to %tmp%.
                    Use this switch to override
    /nofetch        Disables 'git fetch' during version calculation. Might cause
                    GitVersion to not calculate your version as expected.
```
