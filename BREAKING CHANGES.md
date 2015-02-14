v3.0.0
 - Develop no longer tracks master
 - NextVersion.txt has been deprecated, only GitVersion.yaml is supported
 - `AssemblyFileSemVer` variable removed, AssemblyVersioningScheme configuration value makes this variable obsolete
 - Variables `ClassicVersion` and `ClassicVersionWithTag` removed
 - MSBuild task arguments (AssemblyVersioningScheme, DevelopBranchTag, ReleaseBranchTag, TagPrefix, NextVersion) have been removed, use GitVersionConfig.yaml instead
 - GitVersionTask ReleaseDateAttribute no longer has OriginalReleaseDate
