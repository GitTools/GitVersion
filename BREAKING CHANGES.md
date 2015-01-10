v3.0.0
 - `AssemblyFileSemVer` variable removed, AssemblyVersioningScheme configuration value makes this variable obsoluete
 - `ClassicVersion` variable removed, 
 - `ClassicVersionWithTag` variable removed, as above
 - MSBuild task arguments (AssemblyVersioningScheme, DevelopBranchTag, ReleaseBranchTag, TagPrefix, NextVersion) have been removed, use GitVersionConfig.yaml instead