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

```bash
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

`/overrideconfig [key=value]` will override appropriate `key` from 'GitVersion.yml'.

To specify multiple options add multiple `/overrideconfig [key=value]` entries:
`/overrideconfig key1=value1 /overrideconfig key2=value2`.

To have **space characters** as a part of `value`, `value` has be enclosed with double quotes - `key="My value"`.

Double quote character inside of the double quoted `value` has to be be escaped with a backslash '\\' - `key="My \"escaped-quotes\""`.

Following options are supported:

1.  `assembly-file-versioning-format`
2.  `assembly-file-versioning-scheme`
3.  `assembly-informational-format`
4.  `assembly-versioning-format`
5.  `assembly-versioning-scheme`
7.  `commit-date-format`
8.  `commit-message-incrementing`
10. `continuous-delivery-fallback-tag`
11. `increment`
13. `major-version-bump-message`
14. `minor-version-bump-message`
15. `mode`
16. `next-version`
17. `no-bump-message`
18. `patch-version-bump-message`
19. `tag-prefix`
20. `tag-pre-release-weight`
21. `update-build-number`

Read more about [Configuration](/docs/reference/configuration).

Using `override-config` on the command line will not change the contents of the config file `GitVersion.yml`.

### Example: How to override configuration option 'tag-prefix' to use prefix 'custom'

`GitVersion.exe /output json /overrideconfig tag-prefix=custom`

### Example: How to override configuration option 'assembly-versioning-format'

`GitVersion.exe /output json /overrideconfig assembly-versioning-format="{Major}.{Minor}.{Patch}.{env:BUILD_NUMBER ?? 0}"`

Will pickup up environment variable `BUILD_NUMBER` or fallback to zero for assembly revision number.

### Example: How to override configuration option 'assembly-versioning-scheme'

`GitVersion.exe /output json /overrideconfig assembly-versioning-scheme=MajorMinor`

Will use only major and minor version numbers for assembly version. Assembly build and revision numbers will be 0 (e.g. `1.2.0.0`)

### Example: How to override multiple configuration options

`GitVersion.exe /output json /overrideconfig tag-prefix=custom /overrideconfig assembly-versioning-scheme=MajorMinor`

### Example: How to override configuration option 'update-build-number'

`GitVersion.exe /output json /overrideconfig update-build-number=true`

### Example: How to override configuration option 'next-version'

`GitVersion.exe /output json /overrideconfig next-version=6`
