---
Order: 20
Title: Arguments
Description: The supported arguments of the GitVersion Command Line Interface
---

:::{.alert .alert-info}
**Hint:** While documentation and help use `/` as command prefix the hyphen `-`
is supported as well and is a better alternative for usage on \*nix systems.
Example: `-output json` vs. `/output json`
:::

## Help

Below is the output from `gitversion /help` as a best effort to provide
documentation for which arguments GitVersion supports and their meaning.

```
Use convention to derive a SemVer product version from a GitFlow or GitHub based
repository.

GitVersion [path]

    path            The directory containing .git. If not defined current
                    directory is used. (Must be first argument)
    init            Configuration utility for gitversion
    /version        Displays the version of GitVersion
    /diag           Runs GitVersion with additional diagnostic information
                    (requires git.exe to be installed)
    /h or /?        Shows Help

    /targetpath     Same as 'path', but not positional
    /output         Determines the output to the console. Can be either 'json',
                    'file' or 'buildserver', will default to 'json'.
    /outputfile     Path to output file. It is used in combination with /output
                    'file'.
    /showvariable   Used in conjuntion with /output json, will output just a
                    particular variable. E.g. /output json /showvariable SemVer
                    - will output `1.2.3+beta.4`
    /l              Path to logfile.
    /config         Path to config file (defaults to GitVersion.yml)
    /showconfig     Outputs the effective GitVersion config (defaults + custom
                    from GitVersion.yml) in yaml format
    /overrideconfig Overrides GitVersion config values inline (semicolon-
                    separated key value pairs e.g. /overrideconfig
                    tag-prefix=Foo)
                    Currently supported config overrides: tag-prefix
    /nocache        Bypasses the cache, result will not be written to the cache.
    /nonormalize    Disables normalize step on a build server.
    /verbosity      Specifies the amount of information to be displayed.
                    (Quiet, Minimal, Normal, Verbose, Diagnostic)
                    Default is Normal

# AssemblyInfo updating

    /updateassemblyinfo
                    Will recursively search for all 'AssemblyInfo.cs' files in
                    the git repo and update them
    /updateprojectfiles
                    Will recursively search for all project files
                    (.csproj/.vbproj/.fsproj) files in the git repo and update
                    them
                    Note: This is only compatible with the newer Sdk projects
    /updateassemblyinfofilename
                    Specify name of AssemblyInfo file. Can also
                    /updateAssemblyInfo GlobalAssemblyInfo.cs as a shorthand
    /ensureassemblyinfo
                    If the assembly info file specified with
                    /updateassemblyinfo or /updateassemblyinfofilename is not
                    found, it be created with these attributes:
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

gitversion init     Configuration utility for gitversion
```

## Override config

`/overrideconfig [key=value]` will override appropriate key from
`GitVersion.yml`.

At the moment only `tag-prefix` option is supported. Read more about
[Configuration](/docs/reference/configuration). It will not change config file
`GitVersion.yml`.
