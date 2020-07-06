```sh
# Write version to stdout
gitversion --version

# Write help to stdout
gitversion --help

# Normalize the repository to its required state:
gitversion normalize 

# Normalize the repository inside `./project/` to its required state:
gitversion normalize --repository ./project/

# Initialize GitVersion.yml
gitversion config init

# Write the effective GitVersion configuration (defaults + custom from GitVersion.yml) in yaml format to stdout
gitversion config show

# Calculate the version number and output to stdout. Only the JSON with the version variables will go to stdout, errors and warnings will be logged to stderr
gitversion calculate

# Calculate the version number and write it gitversion.json
gitversion calculate > gitversion.json

# Calculate the version number and output to stdout. Include logging information depending on the verbosity level in the logging to stderr.
gitversion calculate --verbosity verbose

# Calculate the version number and output to stdout. Include diagnostics info in the logging to stderr (requires `git` executable on PATH).
gitversion [diag] calculate

# Calculate the version number and log to the file `/var/logs/gitversion.log`
gitversion calculate --logfile /var/logs/gitversion.log

# Calculate the version number based on the configuration file `/etc/gitversion.yml`
gitversion calculate --configfile /etc/gitversion.yml

# Calculate the version and override the `tag-prefix` configuration.
gitversion calculate --override-config tag-prefix=foo

# Calculate the version with caching disabled.
gitversion calculate --no-cache

# Read version variables from stdin and write to globbed AssemblyInfo.cs files
cat gitversion.json | gitversion output --type assemblyinfo --path ./**/AssemblyInfo.cs

# Read version variables from stdin and write to globbed .csproj files
cat gitversion.json | gitversion output --type projectfiles --path ./**/*.csproj

# Read version variables from stdin and write to an auto-detected build server. Without an `--in` argument, stdin is the default input.
cat gitversion.json | gitversion output --type buildserver

# Read version variables from stdin and write to Jenkins.
cat gitversion.json | gitversion output --type buildserver --buildserver Jenkins

# Read version variables from stdin and write to globbed .wxi files.
cat gitversion.json | gitversion output --type wix --path ./**/*.wxi

# Read version variables from stdin and output them to environment variables
cat gitversion.json | gitversion output --type environment

# Read version variables from stdin and output only the `FullSemVer` property to stdout.
cat gitversion.json | gitversion output --property FullSemVer 

# Pipe the output of calculate to gitversion output
gitversion calculate | gitversion output --type assemblyinfo --path ./**/AssemblyInfo.cs

#NOTES [diag] can be used only with calculate command
```