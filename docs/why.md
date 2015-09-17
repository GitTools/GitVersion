# Why use GitVersion
GitVersion makes versioning woes a thing of the past. It looks at your git history to calculate what the version currently is. I have seen and used many different approaches in the past, all have downfalls and often are not transportable between projects.

It solves:

 - Rebuilding tags always produces the same version
 - Not having to rebuild to increment versions
 - Not duplicating version information in multiple places (branch release/2.0.0 already has the version in it, why do I need to change something else)
 - Each branch calculates it's SemVer and versions flow between branches when they are merged
 - Pull requests produce unique pre-release version numbers
 - NuGet semver issues
 - Build server integration
 - Updating assembly info
 - And a whole lot of edge cases you don't want to think about

## Advantages vs other approaches
### Version.txt/Version in build script
 - With version.txt/build script, after the release you need to do an additional commit to bump the version
 - After tagging a release, the next build will still be the same version

### Build Server versioning
 - Cannot have different version numbers on different branches
 - Rebuilding will result in a different build number (if using an auto incrementing number in the version)
 - Need to login to the build server to change version number
 - Only build administrators can change the version number
