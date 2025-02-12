---
Order: 30
Title: Output
Description: Details about the output types supported by the GitVersion CLI
---

By default GitVersion returns a json object to stdout containing all the
[variables](/docs/reference/variables) which GitVersion generates. This works
great if you want to get your build scripts to parse the json object then use
the variables, but there is a simpler way.

`GitVersion.exe /output buildserver` will change the mode of GitVersion to write
out the variables to whatever build server it is running in. You can then use
those variables in your build scripts or run different tools to create versioned
NuGet packages or whatever you would like to do. See [build
servers](/docs/reference/build-servers) for more information about this.

You can even store the [variables](/docs/reference/variables) in a Dotenv file
and load it to have the variables available in your environment.
For that you have to run `GitVersion.exe /output dotenv` and store the output
into e.g. a `gitversion.env` file. These files can also be passed around in CI environments
like [GitHub](https://docs.github.com/en/actions/writing-workflows/choosing-what-your-workflow-does/store-information-in-variables#passing-values-between-steps-and-jobs-in-a-workflow)
or [GitLab](https://docs.gitlab.com/ee/ci/variables/#pass-an-environment-variable-to-another-job).
Below are some examples of using the Dotenv format in the Unix command line:
```bash
# Output version variables in Dotenv format
gitversion /output dotenv

# Show only a subset of the version variables in Dotenv format
gitversion /output dotenv | grep -i "prerelease"

# Show only a subset of the version variables that match the regex in Dotenv format
gitversion /output dotenv | grep -iE "major|sha=|_prerelease"

# Write version variables in Dotenv format into a file
gitversion /output dotenv > gitversion.env
```
